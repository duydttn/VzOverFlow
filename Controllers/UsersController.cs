using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Models;
using VzOverFlow.Models.ViewModels;
using VzOverFlow.Services;

namespace VzOverFlow.Controllers
{
    [Route("users")]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly ITwoFactorService _twoFactorService;

        public UsersController(
            IUserService userService,
            AppDbContext context,
            PasswordHasher<User> passwordHasher,
            ITwoFactorService twoFactorService)
        {
            _userService = userService;
            _context = context;
            _passwordHasher = passwordHasher;
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
            var user = await _context.Users.FindAsync(GetCurrentUserId());
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

            var user = await _context.Users.FindAsync(GetCurrentUserId());
            if (user == null)
            {
                return NotFound();
            }

            var userNameExists = await _context.Users
                .AnyAsync(u => u.Id != user.Id && u.UserName == model.UserName);

            if (userNameExists)
            {
                ModelState.AddModelError(nameof(AccountSettingsViewModel.UserName), "Tên người dùng đã tồn tại.");
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var emailExists = await _context.Users
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
            await _context.SaveChangesAsync();

            TempData["AccountStatusMessage"] = "Đã cập nhật thông tin tài khoản.";
            return RedirectToAction(nameof(AccountSettings));
        }

        [HttpGet("settings/security")]
        [Authorize]
        public async Task<IActionResult> SecuritySettings()
        {
            var user = await _context.Users.FindAsync(GetCurrentUserId());
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
            return View(new User());
        }

        [HttpPost("register")]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register(User user)
        {
            if (await _context.Users.AnyAsync(u => u.UserName == user.UserName))
            {
                ModelState.AddModelError(nameof(VzOverFlow.Models.User.UserName), "Tên người dùng đã tồn tại.");
            }

            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError(nameof(VzOverFlow.Models.User.Email), "Email đã được sử dụng.");
            }

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var plainPassword = user.PasswordHash;
            user.CreatedAt = DateTime.UtcNow;
            user.PasswordHash = _passwordHasher.HashPassword(user, plainPassword);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await SignInUserAsync(user);
            return RedirectToAction(nameof(Profile), new { id = user.Id });
        }

        [HttpGet("login")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin đăng nhập.";
                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == username || u.Email == username);
            if (user == null)
            {
                ViewBag.Error = "Thông tin đăng nhập không chính xác.";
                return View();
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "Thông tin đăng nhập không chính xác.";
                return View();
            }

            if (user.TwoFactorEnabled)
            {
                await _twoFactorService.GenerateAndSendAsync(user, OtpPurpose.Login);
                TempData["TwoFactorUserId"] = user.Id;
                return RedirectToAction(nameof(VerifyLoginOtp));
            }

            await SignInUserAsync(user);
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
            var userId = TempData["TwoFactorUserId"]?.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(nameof(Login));
            }

            if (!await _twoFactorService.ValidateCodeAsync(int.Parse(userId), OtpPurpose.Login, code))
            {
                TempData["TwoFactorError"] = "Mã OTP không hợp lệ hoặc đã hết hạn.";
                TempData.Keep("TwoFactorUserId");
                return RedirectToAction(nameof(VerifyLoginOtp));
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            await SignInUserAsync(user);
            TempData.Remove("TwoFactorUserId");
            return RedirectToAction(nameof(Profile), new { id = user.Id });
        }

        [HttpPost("logout")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpPost("twofactor/request")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestTwoFactorCode(string mode)
        {
            var user = await _context.Users.FindAsync(GetCurrentUserId());
            if (user == null)
            {
                return NotFound();
            }

            var purpose = mode == "disable" ? OtpPurpose.DisableTwoFactor : OtpPurpose.EnableTwoFactor;
            await _twoFactorService.GenerateAndSendAsync(user, purpose);
            TempData["StatusMessage"] = "Mã OTP đã được gửi đến email của bạn.";
            return RedirectToAction(nameof(Security));
        }

        [HttpPost("twofactor/enable")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactor(string code)
        {
            var user = await _context.Users.FindAsync(GetCurrentUserId());
            if (user == null)
            {
                return NotFound();
            }

            if (!await _twoFactorService.ValidateCodeAsync(user.Id, OtpPurpose.EnableTwoFactor, code))
            {
                TempData["StatusMessage"] = "Mã OTP không hợp lệ.";
                return RedirectToAction(nameof(Security));
            }

            user.TwoFactorEnabled = true;
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Đã bật bảo mật 2 lớp.";
            return RedirectToAction(nameof(Security));
        }

        [HttpPost("twofactor/disable")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactor(string code)
        {
            var user = await _context.Users.FindAsync(GetCurrentUserId());
            if (user == null)
            {
                return NotFound();
            }

            if (!await _twoFactorService.ValidateCodeAsync(user.Id, OtpPurpose.DisableTwoFactor, code))
            {
                TempData["StatusMessage"] = "Mã OTP không hợp lệ.";
                return RedirectToAction(nameof(Security));
            }

            user.TwoFactorEnabled = false;
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Đã tắt bảo mật 2 lớp.";
            return RedirectToAction(nameof(Security));
        }

        [HttpPost("password/request-otp")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestChangePasswordOtp()
        {
            var user = await _context.Users.FindAsync(GetCurrentUserId());
            if (user == null)
            {
                return NotFound();
            }

            await _twoFactorService.GenerateAndSendAsync(user, OtpPurpose.ChangePassword);
            TempData["StatusMessage"] = "Mã OTP đổi mật khẩu đã được gửi.";
            return RedirectToAction(nameof(Security));
        }

        [HttpPost("password/change")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["StatusMessage"] = "Thông tin đổi mật khẩu chưa hợp lệ.";
                return RedirectToAction(nameof(Security));
            }

            var user = await _context.Users.FindAsync(GetCurrentUserId());
            if (user == null)
            {
                return NotFound();
            }

            if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.OldPassword) == PasswordVerificationResult.Failed)
            {
                TempData["StatusMessage"] = "Mật khẩu cũ không đúng.";
                return RedirectToAction(nameof(Security));
            }

            if (user.TwoFactorEnabled)
            {
                var valid = await _twoFactorService.ValidateCodeAsync(user.Id, OtpPurpose.ChangePassword, model.OtpCode);
                if (!valid)
                {
                    TempData["StatusMessage"] = "Mã OTP đổi mật khẩu không hợp lệ.";
                    return RedirectToAction(nameof(Security));
                }
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Security));
        }

        private async Task SignInUserAsync(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }
    }
}
