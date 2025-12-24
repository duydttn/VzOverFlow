using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Helpers;
using VzOverFlow.Models;
using VzOverFlow.Services;

namespace VzOverFlow.Controllers
{
    [Authorize]
    public class AnswersController : Controller
    {
        private readonly IQuestionService _questionService;
        private readonly IGamificationService _gamificationService;
        private readonly IVoteService _voteService;
        private readonly AppDbContext _context;

        public AnswersController(
            IQuestionService questionService,
            IGamificationService gamificationService,
            IVoteService voteService,
            AppDbContext context)
        {
            _questionService = questionService;
            _gamificationService = gamificationService;
            _voteService = voteService;
            _context = context;
        }

        public IActionResult Create(int questionId)
        {
            ViewBag.QuestionId = questionId;
            return View(new Answer { QuestionId = questionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int questionId, Answer answer)
        {
            var userId = GetCurrentUserId();

            var lastAnswer = await _context.Answers
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastAnswer != null && (DateTime.UtcNow - lastAnswer.CreatedAt).TotalSeconds < 30)
            {
                TempData["ErrorMessage"] = "Vui lòng đợi 30 giây giữa các lần trả lời.";
                return RedirectToAction("Details", "Questions", new { id = questionId });
            }
            if (SpamChecker.ContainsSpam(answer.Body))
            {
                TempData["ErrorMessage"] = "Nội dung câu trả lời chứa từ khóa không phù hợp.";
                return RedirectToAction("Details", "Questions", new { id = questionId });
            }

            ModelState.Remove("User");
            ModelState.Remove("Question");
            ModelState.Remove("Votes");
            ModelState.Remove("Comments");

            if (!ModelState.IsValid)
            {
                ViewBag.QuestionId = questionId;
                return View(answer);
            }

            answer.UserId = userId;
            var createdAnswer = await _questionService.AddAnswerAsync(questionId, answer);

            await _gamificationService.AwardXpAsync(answer.UserId, ActivityType.AnswerQuestion, questionId, createdAnswer.Id);

            TempData["SuccessMessage"] = "Đã trả lời câu hỏi thành công! +15 XP";
            return RedirectToAction("Details", "Questions", new { id = questionId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var answer = await _questionService.GetAnswerByIdAsync(id);
            if (answer == null) return NotFound();
            return View(answer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Answer answer)
        {
            if (id != answer.Id) return BadRequest();

            if (SpamChecker.ContainsSpam(answer.Body))
            {
                ModelState.AddModelError("", "Nội dung chỉnh sửa chứa từ khóa không phù hợp.");
                return View(answer);
            }

            ModelState.Remove("User");
            ModelState.Remove("Question");
            ModelState.Remove("Votes");
            ModelState.Remove("Comments");

            if (!ModelState.IsValid) return View(answer);

            await _questionService.UpdateAnswerAsync(answer);
            return RedirectToAction("Details", "Questions", new { id = answer.QuestionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var answer = await _questionService.GetAnswerByIdAsync(id);
            if (answer == null) return NotFound();

            await _questionService.DeleteAnswerAsync(id);
            return RedirectToAction("Details", "Questions", new { id = answer.QuestionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VoteAnswer(int answerId, int value)
        {
            try
            {
                var userId = GetCurrentUserId();
                var newScore = await _voteService.VoteAnswerAsync(answerId, userId, value);
                return Json(new { success = true, score = newScore });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim)) throw new InvalidOperationException("User is not authenticated.");
            return int.Parse(idClaim);
        }
    }
}