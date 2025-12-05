using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Models;

namespace VzOverFlow.Controllers
{
    [Route("admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly UserManager<User> _userManager;

        public AdminUsersController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .AsNoTracking()
                .ToListAsync();

            return View(users);
        }

        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == id)
            {
                TempData["ErrorMessage"] = "B?n không th? xóa chính tài kho?n c?a mình.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm th?y ng??i dùng.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"?ã xóa ng??i dùng '{user.UserName}' thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Có l?i x?y ra khi xóa ng??i dùng.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}