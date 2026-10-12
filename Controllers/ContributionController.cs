using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VzOverFlow.Models;
using VzOverFlow.Services;
using System.Security.Claims;
using VzOverFlow.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace VzOverFlow.Controllers
{
    [Authorize]
    public class ContributionController : Controller
    {
        private readonly IReputationService _reputationService;
        private readonly AppDbContext _context;

        public ContributionController(IReputationService reputationService, AppDbContext context)
        {
            _reputationService = reputationService;
            _context = context;
        }

        // POST: /Contribution/Share
        [HttpPost]
        public async Task<IActionResult> Share([FromBody] ShareRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var activity = new UserActivity
            {
                UserId = userId,
                Type = ActivityType.Share,
                CreatedAt = DateTime.UtcNow,
                QuestionId = request.QuestionId, 
                AnswerId = request.AnswerId,
                XpEarned = 0
            };
            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();

            await _reputationService.UpdateForShareAsync(userId);

            return Json(new { success = true, message = "Đã chia sẻ thành công! +1 Uy tín" });
        }

        public class ShareRequest
        {
            public int? QuestionId { get; set; }
            public int? AnswerId { get; set; }
        }

        // POST: /Contribution/RecalculateScores
        // Admin only or Dev tool
        [HttpPost]
        public async Task<IActionResult> RecalculateScores()
        {
            await _reputationService.RecalculateAllUserReputationsAsync();
            return Ok("Recalculation complete.");
        }
    }
}