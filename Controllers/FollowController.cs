using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VzOverFlow.Data;
using VzOverFlow.Models;

namespace VzOverFlow.Controllers
{
    [Authorize]
    public class FollowController : Controller
    {
        private readonly AppDbContext _context;

        public FollowController(AppDbContext context)
        {
            _context = context;
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

                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Message = $"{User.Identity.Name} đã bắt đầu theo dõi bạn.",
                    Link = $"/Users/Profile/{currentUserId}"
                });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, isFollowing });
        }
    }
}