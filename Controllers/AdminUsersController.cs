using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Models;
using VzOverFlow.Models.ViewModels;
using VzOverFlow.Services;

namespace VzOverFlow.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserService _userService;

        public AdminUsersController(UserManager<User> userManager, IUserService userService)
        {
            _userManager = userManager;
            _userService = userService;
        }

        [HttpGet("/admin/users")]
        public async Task<IActionResult> Index(string? search)
        {
            var stats = await _userService.GetAdminDashboardStatsAsync();
            var users = await _userService.GetUsersAsync(search);

            var model = new AdminUsersViewModel
            {
                Stats = stats,
                Users = users,
                SearchQuery = search
            };

            return View(model);
        }

        [HttpPost("/admin/users/toggle-ban/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBan(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == id)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự khóa chính mình.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userService.ToggleUserBanAsync(id);
            if (result)
            {
                TempData["SuccessMessage"] = "Đã thay đổi trạng thái hoạt động của người dùng.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/admin/users/reputation/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustReputation(int id, int amount)
        {
            await _userService.AdjustReputationAsync(id, amount);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/admin/users/role/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int id, string role)
        {
            await _userService.ChangeUserRoleAsync(id, role);
            TempData["SuccessMessage"] = "Đã cập nhật quyền hạn của người dùng.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/admin/users/delete/{id}")]
        [ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == id)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự xóa chính mình.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userService.DeleteUserAsync(id);
            if (result)
            {
                TempData["SuccessMessage"] = "Đã xóa người dùng thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa người dùng.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}