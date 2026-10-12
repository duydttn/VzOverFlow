using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VzOverFlow.Helpers;
using VzOverFlow.Services;

namespace VzOverFlow.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ICommentService _commentService;
        private readonly IUserService _userService;

        public CommentsController(ICommentService commentService, IUserService userService)
        {
            _commentService = commentService;
            _userService = userService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string body, int? questionId, int? answerId)
        {
            if (string.IsNullOrWhiteSpace(body) || (!questionId.HasValue && !answerId.HasValue))
            {
                return BadRequest();
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Challenge();
            int userId = int.Parse(userIdStr);

            // Spam Check
            var user = await _userService.GetUserProfileAsync(userId);
            int reputation = user?.Reputation ?? 0;
            if (SpamChecker.ContainsSpam(body, reputation))
            {
                TempData["ErrorMessage"] = "Nội dung bình luận chứa từ khóa không phù hợp.";
                return RedirectToAction("Details", "Questions", new { id = questionId ?? 0 }); // Fallback
            }

            var comment = await _commentService.AddCommentAsync(body, userId, questionId, answerId);

            if (comment == null)
            {
                return BadRequest();
            }

            // Redirect back to the question details page
            // We need to know the question ID even if it's an answer comment
            int qId = questionId ?? 0;
            if (answerId.HasValue && qId == 0)
            {
                // If we don't have questionId, we might need to find it from the answer
                // But for simplicity in this flow, we'll assume the client passes both or we handle it in view
            }

            return Redirect(Request.Headers["Referer"].ToString() + "#comment-" + comment.Id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Challenge();
            int userId = int.Parse(userIdStr);
            bool isAdmin = User.IsInRole("Admin");

            var result = await _commentService.DeleteCommentAsync(id, userId, isAdmin);
            if (!result) return Forbid();

            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}