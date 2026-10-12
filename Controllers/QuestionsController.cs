using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Helpers;
using VzOverFlow.Models;
using VzOverFlow.Models.ViewModels;
using VzOverFlow.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace VzOverFlow.Controllers
{
    public class QuestionsController : Controller
    {
        private readonly IQuestionService _questionService;
        private readonly IQueryService _queryService;
        private readonly IGamificationService _gamificationService;
        private readonly IVoteService _voteService;
        private readonly IReputationService _reputationService;
        private readonly AppDbContext _context;

        public QuestionsController(
            IQuestionService questionService,
            IQueryService queryService,
            IGamificationService gamificationService,
            IVoteService voteService,
            IReputationService reputationService,
            AppDbContext context)
        {
            _questionService = questionService;
            _queryService = queryService;
            _gamificationService = gamificationService;
            _voteService = voteService;
            _reputationService = reputationService;
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? tag, string? sort)
        {
            var model = await _queryService.GetQuestionsAsync(search, tag, sort);
            return View(model);
        }

        public async Task<IActionResult> Details(int id, string? sortBy = "accepted")
        {
            var model = await _questionService.GetQuestionDetailAsync(id, increaseViewCount: true, sortBy: sortBy);
            if (model == null) return NotFound();
            ViewBag.SortBy = sortBy;
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Create()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user.Reputation < 1)
            {
                TempData["ErrorMessage"] = "Bạn cần ít nhất 1 điểm uy tín để đặt câu hỏi. Hãy tham gia bình luận hoặc trả lời để tích điểm!";
                return RedirectToAction(nameof(Index));
            }

            return View(new QuestionCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> Create(QuestionCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin câu hỏi.";
                return View(model);
            }

            var userId = GetCurrentUserId();

            var lastQuestion = await _context.Questions
                .Where(q => q.UserId == userId)
                .OrderByDescending(q => q.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastQuestion != null && (DateTime.UtcNow - lastQuestion.CreatedAt).TotalSeconds < 60)
            {
                ModelState.AddModelError("", "Bạn thao tác quá nhanh. Vui lòng đợi 60 giây trước khi đặt câu hỏi tiếp theo.");
                return View(model);
            }

            var user = await _context.Users.FindAsync(userId);
            int reputation = user?.Reputation ?? 0;

            if (SpamChecker.ContainsSpam(model.Title, reputation) || SpamChecker.ContainsSpam(model.Body, reputation))
            {
                ModelState.AddModelError("", "Nội dung câu hỏi chứa từ khóa không phù hợp. Vui lòng kiểm tra lại.");
                return View(model);
            }

            try
            {
                var question = new Question
                {
                    Title = model.Title,
                    Body = model.Body,
                    UserId = userId
                };

                await _questionService.CreateQuestionAsync(question, ParseTags(model.Tags));

                TempData["SuccessMessage"] = "Đã đặt câu hỏi thành công! +20 XP";
                return RedirectToAction(nameof(Details), new { id = question.Id });
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error creating question");
                ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.");
                return View(model);
            }
        }

        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var question = await _questionService.GetQuestionEntityAsync(id);
            if (question == null) return NotFound();
            
            if (question.UserId != GetCurrentUserId()) return Forbid();

            var model = new QuestionEditViewModel
            {
                Id = question.Id,
                Title = question.Title,
                Body = question.Body,
                Tags = string.Join(" ", question.Tags.Select(t => t.Name))
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> Edit(int id, QuestionEditViewModel model)
        {
            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid) return View(model);

            var userId = GetCurrentUserId();
            
            // Ownership check
            var existingQuestion = await _questionService.GetQuestionEntityAsync(id);
            if (existingQuestion == null) return NotFound();
            if (existingQuestion.UserId != userId) return Forbid();

            var user = await _context.Users.FindAsync(userId);
            int reputation = user?.Reputation ?? 0;

            if (SpamChecker.ContainsSpam(model.Title, reputation) || SpamChecker.ContainsSpam(model.Body, reputation))
            {
                ModelState.AddModelError("", "Nội dung chỉnh sửa chứa từ khóa không phù hợp.");
                return View(model);
            }

            existingQuestion.Title = model.Title;
            existingQuestion.Body = model.Body;
            // Note: _questionService.UpdateQuestionAsync usually takes Question entity.
            // Be careful here. UpdateQuestionAsync updates tags too.
            // Let's reuse existingQuestion and pass it.
            
            await _questionService.UpdateQuestionAsync(existingQuestion, ParseTags(model.Tags));
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null) return NotFound();
            
            bool isAdmin = User.IsInRole("Admin");
            if (question.UserId != GetCurrentUserId() && !isAdmin) return Forbid();

            await _questionService.DeleteQuestionAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckDuplicate([FromBody] CheckDuplicateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) && string.IsNullOrWhiteSpace(request.Body))
            {
                return BadRequest("Content required");
            }

            var similarQuestions = await _questionService.FindSimilarQuestionsAsync(request.Title ?? "", request.Body ?? "");
            
            var result = similarQuestions.Select(q => new 
            {
                id = q.Id,
                title = q.Title,
                answerCount = q.Answers.Count,
                hasAcceptedAnswer = q.Answers.Any(a => a.IsAccepted),
                url = Url.Action("Details", "Questions", new { id = q.Id })
            });

            return Json(result);
        }

        public class CheckDuplicateRequest
        {
            public string? Title { get; set; }
            public string? Body { get; set; }
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
            catch (Exception)
            {
                // _logger.LogError(ex, ...);
                return Json(new { success = false, message = "Đã xảy ra lỗi hệ thống." });
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
            if (string.IsNullOrEmpty(idClaim)) throw new InvalidOperationException("User is not authenticated.");
            return int.Parse(idClaim);
        }
    }
}