using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Models;

namespace VzOverFlow.Controllers
{
    [Route("chat")]
    [Authorize]
    public class ChatController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public ChatController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var rooms = await _context.ChatRooms
                .Include(r => r.CreatedBy)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Description,
                    r.CreatedAt,
                    CreatedByName = r.CreatedBy.UserName,
                    MessageCount = r.Messages.Count,
                    LastActivity = r.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()
                })
                .ToListAsync();

            // Ensure default lobby exists
            if (!rooms.Any(r => r.Name == "VzOverFlow Lobby"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var lobby = new ChatRoom
                    {
                        Name = "VzOverFlow Lobby",
                        Description = "Phòng chat chung dành cho tất cả thành viên. Giới thiệu bản thân, hỏi đáp nhanh và kết nối với cộng đồng!",
                        CreatedByUserId = user.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ChatRooms.Add(lobby);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(rooms);
        }

        [HttpGet("room/{id:int}")]
        public async Task<IActionResult> Room(int id)
        {
            var room = await _context.ChatRooms
                .Include(r => r.CreatedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
                return NotFound();

            var messages = await _context.ChatMessages
                .Where(m => m.RoomId == id)
                .OrderByDescending(m => m.CreatedAt)
                .Take(50)
                .Include(m => m.User)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    messageType = m.MessageType == ChatMessageType.Text ? "text" : 
                                  m.MessageType == ChatMessageType.Sticker ? "sticker" : 
                                  m.MessageType == ChatMessageType.Image ? "image" : "file",
                    m.Content,
                    m.StickerId,
                    m.FileName,
                    m.FileUrl,
                    m.FileSize,
                    createdAt = m.CreatedAt.ToString("HH:mm"),
                    m.UserId,
                    userName = m.User.UserName,
                    avatarUrl = m.User.AvatarUrl ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={m.User.UserName}"
                })
                .ToListAsync();

            ViewBag.Messages = messages;
            return View(room);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            {
                TempData["Error"] = "Tên phòng không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var room = new ChatRoom
            {
                Name = name.Trim(),
                Description = description?.Trim(),
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatRooms.Add(room);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Room), new { id = room.Id });
        }

        [HttpGet("api/messages/{roomId:int}")]
        public async Task<IActionResult> GetMessages(int roomId, int? before = null)
        {
            var query = _context.ChatMessages
                .Where(m => m.RoomId == roomId);

            if (before.HasValue)
            {
                query = query.Where(m => m.Id < before.Value);
            }

            var messages = await query
                .OrderByDescending(m => m.CreatedAt)
                .Take(50)
                .Include(m => m.User)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    messageType = m.MessageType == ChatMessageType.Text ? "text" : 
                                  m.MessageType == ChatMessageType.Sticker ? "sticker" : 
                                  m.MessageType == ChatMessageType.Image ? "image" : "file",
                    m.Content,
                    m.StickerId,
                    m.FileName,
                    m.FileUrl,
                    m.FileSize,
                    createdAt = m.CreatedAt.ToString("HH:mm"),
                    m.UserId,
                    userName = m.User.UserName,
                    avatarUrl = m.User.AvatarUrl ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={m.User.UserName}"
                })
                .ToListAsync();

            return Json(messages);
        }

        [HttpPost("api/upload/{roomId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(int roomId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file provided" });

            if (file.Length > 5 * 1024 * 1024) // 5MB limit
                return BadRequest(new { error = "File size exceeds 5MB limit" });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Allowed extensions
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt", ".zip" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return BadRequest(new { error = "File type not allowed" });

            // Save file
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "chat");
            Directory.CreateDirectory(uploadsDir);

            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, uniqueFileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"/uploads/chat/{uniqueFileName}";
            var isImage = new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(ext);

            var message = new ChatMessage
            {
                MessageType = isImage ? ChatMessageType.Image : ChatMessageType.File,
                FileName = file.FileName,
                FileUrl = fileUrl,
                FileSize = file.Length,
                UserId = user.Id,
                RoomId = roomId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = new
                {
                    id = message.Id,
                    messageType = isImage ? "image" : "file",
                    fileName = message.FileName,
                    fileUrl = message.FileUrl,
                    fileSize = message.FileSize,
                    createdAt = message.CreatedAt.ToString("HH:mm"),
                    userId = user.Id,
                    userName = user.UserName,
                    avatarUrl = user.AvatarUrl ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={user.UserName}"
                }
            });
        }
    }
}