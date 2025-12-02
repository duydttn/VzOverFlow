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

        public QuestionsController(IQuestionService questionService)
        {
            _questionService = questionService;
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
            if (!ModelState.IsValid)
            {
                return View(question);
            }

            question.UserId = GetCurrentUserId();
            await _questionService.CreateQuestionAsync(question, ParseTags(tags));
            return RedirectToAction(nameof(Details), new { id = question.Id });
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
