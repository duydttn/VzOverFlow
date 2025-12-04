using System;
using System.Linq;
using System.Security.Claims;
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
    [Route("users")]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ITwoFactorService _twoFactorService;

        public UsersController(
            IUserService userService,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ITwoFactorService twoFactorService)
        {
            _userService = userService;
            _userManager = userManager;
            _signInManager = signInManager;
            _twoFactorService = twoFactorService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? search)
        {
            var users = await _userService.GetUsersAsync(search);
            return View(users);
        }

        [HttpGet("profile/{id:int}")]
        public async Task<IActionResult> Profile(int id)
        {
            var profile = await _userService.GetUserProfileAsync(id);
            if (profile == null)
            {
                return NotFound();
            }

            return View(profile);
        }

        [HttpGet("settings/account")]
        [Authorize]
        public async Task<IActionResult> AccountSettings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new AccountSettingsViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                StatusMessage = TempData["AccountStatusMessage"] as string
            };

            return View("SettingsAccount", model);
        }

        [HttpPost("settings/account")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AccountSettings(AccountSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("SettingsAccount", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var userNameExists = await _userManager.Users
                .AnyAsync(u => u.Id != user.Id && u.UserName == model.UserName);

            if (userNameExists)
            {
                ModelState.AddModelError(nameof(AccountSettingsViewModel.UserName), "Tên người dùng đã tồn tại.");
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var emailExists = await _userManager.Users
                    .AnyAsync(u => u.Id != user.Id && u.Email == model.Email);

                if (emailExists)
                {
                    ModelState.AddModelError(nameof(AccountSettingsViewModel.Email), "Email đã được sử dụng.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View("SettingsAccount", model);
            }

            user.UserName = model.UserName.Trim();
            user.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View("SettingsAccount", model);
            }

            TempData["AccountStatusMessage"] = "Đã cập nhật thông tin tài khoản.";
            return RedirectToAction(nameof(AccountSettings));
        }

        [HttpGet("settings/security")]
        [Authorize]
        public async Task<IActionResult> SecuritySettings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new SecuritySettingsViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                TwoFactorEnabled = user.TwoFactorEnabled,
                StatusMessage = TempData["StatusMessage"] as string
            };

            return View("SettingsSecurity", model);
        }

        [HttpGet("register")]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost("register")]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userName = model.UserName.Trim();
            var email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();

            if (await _userManager.Users.AnyAsync(u => u.UserName == userName))
            {
                ModelState.AddModelError(nameof(RegisterViewModel.UserName), "Tên người dùng đã tồn tại.");
                return View(model);
            }

            if (!string.IsNullOrEmpty(email) && await _userManager.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError(nameof(RegisterViewModel.Email), "Email đã được sử dụng.");
                return View(model);
            }

            var user = new User
            {
                UserName = userName,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction(nameof(Profile), new { id = user.Id });
        }

        [HttpGet("login")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var identifier = model.UserNameOrEmail.Trim();
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == identifier || u.Email == identifier);
            if (user == null)
            {
                ViewBag.Error = "Thông tin đăng nhập không chính xác.";
                return View(model);
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordValid)
            {
                ViewBag.Error = "Thông tin đăng nhập không chính xác.";
                return View(model);
            }

            if (user.TwoFactorEnabled)
            {
                await _twoFactorService.GenerateAndSendAsync(user, OtpPurpose.Login);
                TempData["TwoFactorUserId"] = user.Id;
                return RedirectToAction(nameof(VerifyLoginOtp));
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "AdminUsers");
            }

            return RedirectToAction(nameof(Profile), new { id = user.Id });
        }

        [HttpGet("verify-otp")]
        [AllowAnonymous]
        public IActionResult VerifyLoginOtp()
        {
            if (TempData["TwoFactorUserId"] == null)
            {
                return RedirectToAction(nameof(Login));
            }

            TempData.Keep("TwoFactorUserId");
            ViewBag.Error = TempData["TwoFactorError"];
            return View();
        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyLoginOtp(string code)
        {
            var userIdValue = TempData["TwoFactorUserId"]?.ToString();
            if (string.IsNullOrEmpty(userIdValue))
            {
                return RedirectToAction(nameof(Login));
            }

            if (!await _twoFactorService.ValidateCodeAsync(int.Parse(userIdValue), OtpPurpose.Login, code))
            {
                TempData["TwoFactorError"] = "Mã OTP không hợp lệ hoặc đã hết hạn.";
                TempData.Keep("TwoFactorUserId");
                return RedirectToAction(nameof(VerifyLoginOtp));
            }

            var user = await _userManager.FindByIdAsync(userIdValue);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData.Remove("TwoFactorUserId");
            return RedirectToAction(nameof(Profile), new { id = user.Id });
        }

        [HttpPost("logout")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpPost("twofactor/request")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestTwoFactorCode(string mode)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var purpose = mode == "disable" ? OtpPurpose.DisableTwoFactor : OtpPurpose.EnableTwoFactor;
            await _twoFactorService.GenerateAndSendAsync(user, purpose);
            TempData["StatusMessage"] = "Mã OTP đã được gửi đến email của bạn.";
            return RedirectToAction(nameof(SecuritySettings));
        }

        [HttpPost("twofactor/enable")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactor(string code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (!await _twoFactorService.ValidateCodeAsync(user.Id, OtpPurpose.EnableTwoFactor, code))
            {
                TempData["StatusMessage"] = "Mã OTP không hợp lệ.";
                return RedirectToAction(nameof(SecuritySettings));
            }

            user.TwoFactorEnabled = true;
            await _userManager.UpdateAsync(user);
            TempData["StatusMessage"] = "Đã bật bảo mật 2 lớp.";
            return RedirectToAction(nameof(SecuritySettings));
        }

        [HttpPost("twofactor/disable")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactor(string code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (!await _twoFactorService.ValidateCodeAsync(user.Id, OtpPurpose.DisableTwoFactor, code))
            {
                TempData["StatusMessage"] = "Mã OTP không hợp lệ.";
                return RedirectToAction(nameof(SecuritySettings));
            }

            user.TwoFactorEnabled = false;
            await _userManager.UpdateAsync(user);
            TempData["StatusMessage"] = "Đã tắt bảo mật 2 lớp.";
            return RedirectToAction(nameof(SecuritySettings));
        }

        [HttpPost("password/request-otp")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestChangePasswordOtp()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            await _twoFactorService.GenerateAndSendAsync(user, OtpPurpose.ChangePassword);
            TempData["StatusMessage"] = "Mã OTP đổi mật khẩu đã được gửi.";
            return RedirectToAction(nameof(SecuritySettings));
        }

        [HttpPost("password/change")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["StatusMessage"] = "Thông tin đổi mật khẩu chưa hợp lệ.";
                return RedirectToAction(nameof(SecuritySettings));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (!await _userManager.CheckPasswordAsync(user, model.OldPassword))
            {
                TempData["StatusMessage"] = "Mật khẩu cũ không đúng.";
                return RedirectToAction(nameof(SecuritySettings));
            }

            if (user.TwoFactorEnabled)
            {
                var valid = await _twoFactorService.ValidateCodeAsync(user.Id, OtpPurpose.ChangePassword, model.OtpCode);
                if (!valid)
                {
                    TempData["StatusMessage"] = "Mã OTP đổi mật khẩu không hợp lệ.";
                    return RedirectToAction(nameof(SecuritySettings));
                }
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            TempData["StatusMessage"] = result.Succeeded
                ? "Đổi mật khẩu thành công."
                : string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(SecuritySettings));
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
        }
        }

    }
}
