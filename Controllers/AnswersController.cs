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

        public AnswersController(IQuestionService questionService)
        {
            _questionService = questionService;
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
            if (!ModelState.IsValid)
            {
                ViewBag.QuestionId = questionId;
                return View(answer);
            }

            answer.UserId = GetCurrentUserId();
            await _questionService.AddAnswerAsync(questionId, answer);
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
