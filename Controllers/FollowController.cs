using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VzOverFlow.Data;
using VzOverFlow.Models;
using VzOverFlow.Services;

namespace VzOverFlow.Controllers
{
    [Authorize]
    public class FollowController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public FollowController(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (currentUserId == userId) return BadRequest("Không thể tự theo dõi.");

            var follow = await _context.UserFollows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowedUserId == userId);

            bool isFollowing = false;

            if (follow != null)
            {
                _context.UserFollows.Remove(follow);
            }
            else
            {
                _context.UserFollows.Add(new UserFollow { FollowerId = currentUserId, FollowedUserId = userId });
                isFollowing = true;

                await _notificationService.NotifyFollowAsync(userId, User.Identity.Name, currentUserId);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, isFollowing });
        }
    }
}