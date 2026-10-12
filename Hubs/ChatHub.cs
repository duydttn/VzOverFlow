using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Models;
using System.Security.Claims;

namespace VzOverFlow.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        private static readonly Dictionary<string, HashSet<string>> _roomConnections = new();
        private static readonly Dictionary<string, (int UserId, string UserName)> _connectionUsers = new();

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = Context.User?.Identity?.Name ?? "Anonymous";
            
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int uid))
            {
                _connectionUsers[Context.ConnectionId] = (uid, userName);
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            foreach (var room in _roomConnections)
            {
                room.Value.Remove(Context.ConnectionId);
            }
            
            _connectionUsers.Remove(Context.ConnectionId);
            
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinRoom(int roomId)
        {
            var roomGroup = $"room_{roomId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, roomGroup);
            
            if (!_roomConnections.ContainsKey(roomGroup))
            {
                _roomConnections[roomGroup] = new HashSet<string>();
            }
            _roomConnections[roomGroup].Add(Context.ConnectionId);
            
            // Notify others
            if (_connectionUsers.TryGetValue(Context.ConnectionId, out var user))
            {
                await Clients.Group(roomGroup).SendAsync("UserJoined", new { 
                    userId = user.UserId, 
                    userName = user.UserName 
                });
            }
            
            // Send online users list
            var onlineUsers = _roomConnections[roomGroup]
                .Where(c => _connectionUsers.ContainsKey(c))
                .Select(c => _connectionUsers[c])
                .Distinct()
                .ToList();
                
            await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);
        }

        public async Task LeaveRoom(int roomId)
        {
            var roomGroup = $"room_{roomId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomGroup);
            
            if (_roomConnections.ContainsKey(roomGroup))
            {
                _roomConnections[roomGroup].Remove(Context.ConnectionId);
            }
            
            if (_connectionUsers.TryGetValue(Context.ConnectionId, out var user))
            {
                await Clients.Group(roomGroup).SendAsync("UserLeft", new { 
                    userId = user.UserId, 
                    userName = user.UserName 
                });
            }
        }

        public async Task SendMessage(int roomId, string content)
        {
            if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
                return;

            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int uid))
                return;

            var user = await _context.Users.FindAsync(uid);
            if (user == null) return;

            var message = new ChatMessage
            {
                Content = content.Trim(),
                MessageType = ChatMessageType.Text,
                UserId = uid,
                RoomId = roomId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            var roomGroup = $"room_{roomId}";
            await Clients.Group(roomGroup).SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                messageType = "text",
                content = message.Content,
                createdAt = message.CreatedAt.ToString("HH:mm"),
                userId = user.Id,
                userName = user.UserName,
                avatarUrl = user.AvatarUrl ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={user.UserName}"
            });
        }

        public async Task SendSticker(int roomId, string stickerId)
        {
            if (string.IsNullOrWhiteSpace(stickerId) || stickerId.Length > 50)
                return;

            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int uid))
                return;

            var user = await _context.Users.FindAsync(uid);
            if (user == null) return;

            var message = new ChatMessage
            {
                MessageType = ChatMessageType.Sticker,
                StickerId = stickerId,
                UserId = uid,
                RoomId = roomId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            var roomGroup = $"room_{roomId}";
            await Clients.Group(roomGroup).SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                messageType = "sticker",
                stickerId = stickerId,
                createdAt = message.CreatedAt.ToString("HH:mm"),
                userId = user.Id,
                userName = user.UserName,
                avatarUrl = user.AvatarUrl ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={user.UserName}"
            });
        }
    }
}