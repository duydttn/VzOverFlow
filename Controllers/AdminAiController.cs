using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VzOverFlow.Models;
using VzOverFlow.Models.ViewModels;
using VzOverFlow.Services;

namespace VzOverFlow.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin/ai")]
    public class AdminAiController : Controller
    {
        private readonly IAiConfigService _aiConfigService;

        public AdminAiController(IAiConfigService aiConfigService)
        {
            _aiConfigService = aiConfigService;
        }

        public async Task<IActionResult> Index()
        {
            var configs = await _aiConfigService.GetAllConfigsAsync();
            return View(configs);
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new AiConfigViewModel());
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AiConfigViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var config = new AiConfig
            {
                Provider = model.Provider,
                ApiKey = model.ApiKey,
                ModelName = model.ModelName,
                IsActive = model.IsActive,
                Note = model.Note
            };

            await _aiConfigService.AddConfigAsync(config);
            TempData["SuccessMessage"] = "Đã thêm cấu hình AI mới.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var config = await _aiConfigService.GetConfigByIdAsync(id);
            if (config == null) return NotFound();

            var model = new AiConfigViewModel
            {
                Id = config.Id,
                Provider = config.Provider,
                ApiKey = config.ApiKey,
                ModelName = config.ModelName,
                IsActive = config.IsActive,
                Note = config.Note
            };

            return View(model);
        }

        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AiConfigViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var config = await _aiConfigService.GetConfigByIdAsync(id);
            if (config == null) return NotFound();

            config.Provider = model.Provider;
            config.ApiKey = model.ApiKey;
            config.ModelName = model.ModelName;
            config.IsActive = model.IsActive;
            config.Note = model.Note;

            await _aiConfigService.UpdateConfigAsync(config);
            TempData["SuccessMessage"] = "Đã cập nhật cấu hình AI.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("toggle/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            await _aiConfigService.ToggleConfigStatusAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _aiConfigService.DeleteConfigAsync(id);
            TempData["SuccessMessage"] = "Đã xóa cấu hình AI.";
            return RedirectToAction(nameof(Index));
        }
    }
}