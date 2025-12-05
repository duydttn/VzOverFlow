using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public AnswersController(IQuestionService questionService, IGamificationService gamificationService, IVoteService voteService)
        {
            _questionService = questionService;
            _gamificationService = gamificationService;
            _voteService = voteService;
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
            // Remove navigation properties from validation
            ModelState.Remove("User");
            ModelState.Remove("Question");
            ModelState.Remove("Votes");
            ModelState.Remove("Comments");

            if (!ModelState.IsValid)
            {
                ViewBag.QuestionId = questionId;
                return View(answer);
            }

            answer.UserId = GetCurrentUserId();
            var createdAnswer = await _questionService.AddAnswerAsync(questionId, answer);

            // Award XP for answering
            await _gamificationService.AwardXpAsync(answer.UserId, ActivityType.AnswerQuestion, questionId, createdAnswer.Id);

            TempData["SuccessMessage"] = "Đã trả lời câu hỏi thành công! +15 XP";
            return RedirectToAction("Details", "Questions", new { id = questionId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var answer = await _questionService.GetAnswerByIdAsync(id);
            if (answer == null)
            {
                return NotFound();
            }

            return View(answer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Answer answer)
        {
            if (id != answer.Id)
            {
                return BadRequest();
            }

            // Remove navigation properties from validation
            ModelState.Remove("User");
            ModelState.Remove("Question");
            ModelState.Remove("Votes");
            ModelState.Remove("Comments");

            if (!ModelState.IsValid)
            {
                return View(answer);
            }

            await _questionService.UpdateAnswerAsync(answer);
            return RedirectToAction("Details", "Questions", new { id = answer.QuestionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var answer = await _questionService.GetAnswerByIdAsync(id);
            if (answer == null)
            {
                return NotFound();
            }

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
            if (string.IsNullOrEmpty(idClaim))
            {
                throw new InvalidOperationException("User is not authenticated.");
            }

            return int.Parse(idClaim);
        }
    }
}
