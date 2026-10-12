using Microsoft.AspNetCore.Mvc;
using VzOverFlow.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace VzOverFlow.ViewComponents
{
    public class DailyMissionsViewComponent : ViewComponent
    {
        private readonly IGamificationService _gamificationService;

        public DailyMissionsViewComponent(IGamificationService gamificationService)
        {
            _gamificationService = gamificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Content(string.Empty);
            }

            var claimsPrincipal = User as ClaimsPrincipal;
            var userIdClaim = claimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Content(string.Empty);
            }

            var dailyMission = await _gamificationService.GetTodayMissionAsync(userId);
            return View(dailyMission);
        }
    }
}