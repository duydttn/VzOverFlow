using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VzOverFlow.Models;
using VzOverFlow.Models.ViewModels;
using VzOverFlow.Services;

namespace VzOverFlow.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
 private readonly IReportService _reportService;
        private readonly IQuestionService _questionService;

        public ReportsController(IReportService reportService, IQuestionService questionService)
      {
     _reportService = reportService;
       _questionService = questionService;
 }

        [HttpGet]
 public async Task<IActionResult> Create(int? questionId, int? answerId)
 {
    if (questionId == null && answerId == null)
      {
     return BadRequest("Ph?i ch? ??nh câu h?i ho?c câu tr? l?i ?? báo cáo");
         }

  var userId = GetCurrentUserId();
       
    // Check if user already reported this content
        if (await _reportService.HasUserReportedAsync(userId, questionId, answerId))
  {
    TempData["ErrorMessage"] = "B?n ?ã báo cáo n?i dung này tr??c ?ó.";
     return RedirectToAction(questionId.HasValue ? "Details" : "Index", "Questions", 
 questionId.HasValue ? new { id = questionId } : null);
         }

   var model = new CreateReportViewModel
       {
    QuestionId = questionId,
    AnswerId = answerId
       };

            // Get content preview
if (questionId.HasValue)
 {
      var question = await _questionService.GetQuestionDetailAsync(questionId.Value);
          if (question != null)
     {
      model.ContentPreview = question.Question.Title;
    model.ContentType = "Câu h?i";
    }
         }
    else if (answerId.HasValue)
      {
         var answer = await _questionService.GetAnswerByIdAsync(answerId.Value);
      if (answer != null)
    {
    model.ContentPreview = answer.Body.Substring(0, Math.Min(100, answer.Body.Length)) + "...";
        model.ContentType = "Câu tr? l?i";
     }
      }

      return View(model);
        }

        [HttpPost]
  [ValidateAntiForgeryToken]
   public async Task<IActionResult> Create(CreateReportViewModel model)
   {
  if (!ModelState.IsValid)
       {
   return View(model);
      }

     try
    {
 var userId = GetCurrentUserId();
          await _reportService.CreateReportAsync(userId, model);

TempData["SuccessMessage"] = "?ã g?i báo cáo thành công. Chúng tôi s? xem xét trong th?i gian s?m nh?t.";
   
         if (model.QuestionId.HasValue)
     {
    return RedirectToAction("Details", "Questions", new { id = model.QuestionId });
    }
   else
   {
  var answer = await _questionService.GetAnswerByIdAsync(model.AnswerId!.Value);
          return RedirectToAction("Details", "Questions", new { id = answer?.QuestionId });
             }
        }
    catch (Exception ex)
  {
         ModelState.AddModelError("", $"Có l?i x?y ra: {ex.Message}");
     return View(model);
            }
 }

   // Admin only - View all reports
[HttpGet]
 [Authorize(Roles = "Admin")]
 public async Task<IActionResult> Index(ReportStatus? status)
{
       var model = await _reportService.GetReportsAsync(status);
      return View(model);
 }

   // Admin only - View report details and resolve
        [HttpGet]
 [Authorize(Roles = "Admin")]
 public async Task<IActionResult> Details(int id)
 {
       var report = await _reportService.GetReportByIdAsync(id);
  if (report == null)
{
    return NotFound();
       }

return View(report);
        }

        [HttpPost]
   [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
 public async Task<IActionResult> Resolve(int id, ReportStatus status, string? resolution)
        {
try
      {
    var userId = GetCurrentUserId();
         await _reportService.UpdateReportStatusAsync(id, userId, status, resolution);
         TempData["SuccessMessage"] = "?ã c?p nh?t tr?ng thái báo cáo.";
     return RedirectToAction(nameof(Index));
         }
    catch (Exception ex)
      {
     TempData["ErrorMessage"] = $"Có l?i x?y ra: {ex.Message}";
      return RedirectToAction(nameof(Details), new { id });
        }
 }

     private int GetCurrentUserId()
        {
       var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
   }
 }
}
