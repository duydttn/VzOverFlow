using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VzOverFlow.Models;
using VzOverFlow.Services;

namespace VzOverFlow.Controllers
{
    public class QuestionsController : Controller
    {
        private readonly IQuestionService _questionService;
        private readonly IGamificationService _gamificationService;
        private readonly IVoteService _voteService;

        public QuestionsController(IQuestionService questionService, IGamificationService gamificationService, IVoteService voteService)
        {
            _questionService = questionService;
            _gamificationService = gamificationService;
            _voteService = voteService;
        }

        public async Task<IActionResult> Index(string? search, string? tag, string? sort)
        {
            var model = await _questionService.GetQuestionsAsync(search, tag, sort);
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var model = await _questionService.GetQuestionDetailAsync(id, increaseViewCount: true);
            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [Authorize]
        public IActionResult Create()
        {
            return View(new Question());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(Question question, string? tags)
        {
            // Remove User navigation property from validation
            ModelState.Remove("User");
            ModelState.Remove("AcceptedAnswer");
            ModelState.Remove("Answers");
            ModelState.Remove("Comments");
            ModelState.Remove("Tags");
            ModelState.Remove("Votes");

            if (!ModelState.IsValid)
            {
                ViewBag.Tags = tags;
                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin câu hỏi.";
                return View(question);
            }

            try
            {
                question.UserId = GetCurrentUserId();
                var createdQuestion = await _questionService.CreateQuestionAsync(question, ParseTags(tags));
                
                // Award XP for asking question
                await _gamificationService.AwardXpAsync(question.UserId, ActivityType.AskQuestion, createdQuestion.Id);
            
                TempData["SuccessMessage"] = "Đã đăng câu hỏi thành công! +10 XP";
                return RedirectToAction(nameof(Details), new { id = createdQuestion.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
                ViewBag.Tags = tags;
                return View(question);
            }
        }

        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var question = await _questionService.GetQuestionEntityAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            ViewBag.Tags = string.Join(" ", question.Tags.Select(t => t.Name));
            return View(question);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, Question question, string? tags)
        {
            if (id != question.Id)
            {
                return BadRequest();
            }

            // Remove navigation properties from validation
            ModelState.Remove("User");
            ModelState.Remove("AcceptedAnswer");
            ModelState.Remove("Answers");
            ModelState.Remove("Comments");
            ModelState.Remove("Tags");
            ModelState.Remove("Votes");

            if (!ModelState.IsValid)
            {
                ViewBag.Tags = tags;
                return View(question);
            }

            await _questionService.UpdateQuestionAsync(question, ParseTags(tags));
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            await _questionService.DeleteQuestionAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> VoteQuestion(int questionId, int value)
        {
            try
            {
                var userId = GetCurrentUserId();
                var newScore = await _voteService.VoteQuestionAsync(questionId, userId, value);
                return Json(new { success = true, score = newScore });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private static List<string> ParseTags(string? tags)
        {
            return tags?
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant())
                .Distinct()
                .ToList() ?? new List<string>();
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
