using System;
using System.IO;
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
        private readonly IAuthenticatorService _authenticatorService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ITwoFactorService twoFactorService,
            IAuthenticatorService authenticatorService,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _userManager = userManager;
            _signInManager = signInManager;
            _twoFactorService = twoFactorService;
            _authenticatorService = authenticatorService;
            _logger = logger;
        }

        [HttpGet("banned")]
        public IActionResult Banned()
        {
            return View();
        }

        [HttpGet("/me")]
        [Authorize]
        public IActionResult Me()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                 return Challenge();
            }
            return RedirectToAction("Profile", new { id = int.Parse(userId) });
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
            var currentUserIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int currentUserId = int.TryParse(currentUserIdStr, out var uid) ? uid : 0;

            var profile = await _userService.GetUserProfileAsync(id, currentUserId);
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
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl,
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
            user.Bio = model.Bio;

            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.AvatarFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AvatarFile.CopyToAsync(stream);
                }
                user.AvatarUrl = "/images/avatars/" + uniqueFileName;
            }

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
        public async Task<IActionResult> SecuritySettings(bool generateKey = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (generateKey && string.IsNullOrEmpty(user.AuthenticatorKey) && !user.TwoFactorEnabled)
            {
                user.AuthenticatorKey = _authenticatorService.GenerateAuthenticatorKey();
                await _userManager.UpdateAsync(user);
            }

            var model = new SecuritySettingsViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                TwoFactorEnabled = user.TwoFactorEnabled,
                StatusMessage = TempData["StatusMessage"] as string
            };

            if (!user.TwoFactorEnabled && !string.IsNullOrEmpty(user.AuthenticatorKey) && !string.IsNullOrEmpty(user.Email))
            {
                model.AuthenticatorKey = user.AuthenticatorKey;
                var qrCodeUri = _authenticatorService.GenerateQrCodeUri(user.Email, user.AuthenticatorKey);
                model.QrCodeDataUrl = _authenticatorService.GenerateQrCodeImage(qrCodeUri);
                model.FormattedKey = _authenticatorService.FormatKeyForManualEntry(user.AuthenticatorKey);
            }

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

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(nameof(RegisterViewModel.Email), "Email là bắt buộc để nhận mã xác thực.");
                return View(model);
            }

            var user = new User
            {
                UserName = userName,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                TwoFactorEnabled = false,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            await _twoFactorService.GenerateAndSendAsync(user, OtpPurpose.EmailVerification);
     
            TempData["PendingUserId"] = user.Id;
            TempData["PendingUserEmail"] = user.Email;
            TempData["InfoMessage"] = "Một mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra và nhập mã để hoàn tất đăng ký.";
          
            return RedirectToAction(nameof(VerifyEmail), new { userId = user.Id });
        }

        [HttpGet("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail(int? userId)
        {
            if (userId == null)
            {
                if (TempData["PendingUserId"] != null && int.TryParse(TempData["PendingUserId"].ToString(), out int tdId))
                {
                    userId = tdId;
                }
            }

            if (userId == null)
            {
                return RedirectToAction(nameof(Register));
            }

            var user = await _userManager.FindByIdAsync(userId.Value.ToString());
            if (user == null)
            {
                return RedirectToAction(nameof(Register));
            }

            ViewBag.UserId = userId.Value;
            ViewBag.Email = user.Email;
            ViewBag.Info = TempData["InfoMessage"];
            ViewBag.Error = TempData["ErrorMessage"];

            return View();
        }

        [HttpPost("verify-email")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(string otpCode, int? userId)
        {
            if (userId == null)
            {
                var userIdValue = TempData.Peek("PendingUserId")?.ToString();
                if (!string.IsNullOrEmpty(userIdValue) && int.TryParse(userIdValue, out int tdId))
                {
                    userId = tdId;
                }
            }

            if (userId == null)
            {
                return RedirectToAction(nameof(Register));
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return RedirectToAction(nameof(Register));
            }

            if (!await _twoFactorService.ValidateCodeAsync(userId.Value, OtpPurpose.EmailVerification, otpCode))
            {
                TempData["ErrorMessage"] = "Mã OTP không hợp lệ hoặc đã hết hạn. Vui lòng thử lại.";
                TempData["PendingUserId"] = userId;
                TempData["PendingUserEmail"] = user.Email;
                return RedirectToAction(nameof(VerifyEmail), new { userId = userId });
            }

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            await _signInManager.SignInAsync(user, isPersistent: false);
            
            TempData.Remove("PendingUserId");
            TempData.Remove("PendingUserEmail");
            TempData["SuccessMessage"] = $"Chào mừng {user.UserName}! Đăng ký thành công. Email của bạn đã được xác thực.";
            TempData["Celebration"] = true;
            
            return RedirectToAction("Index", "Home");
        }

        [HttpPost("resend-email-verification")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailVerification(int? userId)
        {
            if (userId == null)
            {
               var userIdValue = TempData["PendingUserId"]?.ToString();
               if (!string.IsNullOrEmpty(userIdValue) && int.TryParse(userIdValue, out int tdId))
               {
                   userId = tdId;
               }
            }

            if (userId == null)
            {
                return RedirectToAction(nameof(Register));
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return RedirectToAction(nameof(Register));
            }
            await _twoFactorService.InvalidateCodesAsync(userId.Value, OtpPurpose.EmailVerification);
            await _twoFactorService.GenerateAndSendAsync(user, OtpPurpose.EmailVerification);
            
            // Should reuse the passed userId
            TempData["PendingUserId"] = userId;
            TempData["PendingUserEmail"] = user.Email;
            TempData["InfoMessage"] = "Mã OTP mới đã được gửi đến email của bạn.";

            return RedirectToAction(nameof(VerifyEmail), new { userId = userId });
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

            if (user.IsBanned)
            {
                ViewBag.Error = "Tài khoản của bạn đã bị khóa bởi quản trị viên. Vui lòng liên hệ để biết thêm chi tiết.";
                return View(model);
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordValid)
            {
                ViewBag.Error = "Thông tin đăng nhập không chính xác.";
                return View(model);
            }

            if (string.IsNullOrEmpty(user.Email))
            {
                ViewBag.Error = "Tài khoản chưa có email. Vui lòng liên hệ quản trị viên.";
                return View(model);
            }

            if (user.TwoFactorEnabled && !string.IsNullOrEmpty(user.AuthenticatorKey))
            {
                TempData["TwoFactorUserId"] = user.Id;
                TempData["InfoMessage"] = "Vui lòng nhập mã từ ứng dụng xác thực của bạn.";
                return RedirectToAction(nameof(VerifyAuthenticatorCode));
            }

            await _twoFactorService.GenerateAndSendAsync(user, OtpPurpose.Login);
            TempData["LoginUserId"] = user.Id;
            TempData["LoginUserEmail"] = user.Email;
            TempData["InfoMessage"] = "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra và nhập mã để đăng nhập.";

            return RedirectToAction(nameof(VerifyLoginOtp));
        }


        [HttpGet("verify-authenticator")]
        [AllowAnonymous]
        public IActionResult VerifyAuthenticatorCode()
        {
            if (TempData["TwoFactorUserId"] == null)
            {
                return RedirectToAction(nameof(Login));
            }

            TempData.Keep("TwoFactorUserId");
            ViewBag.Error = TempData["TwoFactorError"];
            ViewBag.Info = TempData["InfoMessage"];
            return View();
        }

        [HttpPost("verify-authenticator")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyAuthenticatorCode(string code)
        {
            var userIdValue = TempData.Peek("TwoFactorUserId")?.ToString();
            if (string.IsNullOrEmpty(userIdValue))
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(userIdValue);
            if (user == null || string.IsNullOrEmpty(user.AuthenticatorKey))
            {
                return RedirectToAction(nameof(Login));
            }

            try
            {
                if (!_authenticatorService.ValidateTwoFactorCode(user.AuthenticatorKey, code))
                {
                    ViewBag.Error = "Mã xác thực không hợp lệ. Vui lòng thử lại.";
                    return View();
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData.Remove("TwoFactorUserId");
                return RedirectToAction(nameof(Profile), new { id = user.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in VerifyAuthenticatorCode");
                ViewBag.Error = $"SYSTEM ERROR: {ex.Message}";
                return View();
            }
        }

        [HttpPost("login-email-backup")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWithEmailBackup()
        {
            var userIdValue = TempData.Peek("TwoFactorUserId")?.ToString();
            if (string.IsNullOrEmpty(userIdValue))
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(userIdValue);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            try 
            {
                // Gửi OTP qua email
                await _twoFactorService.GenerateAndSendAsync(user, OtpPurpose.Login);
                
                TempData["LoginUserId"] = user.Id;
                TempData["LoginUserEmail"] = user.Email;
                TempData["InfoMessage"] = "Mã OTP dự phòng đã được gửi đến email của bạn. Vui lòng kiểm tra và nhập mã.";
                
                // Sau khi chuyển sang email OTP, có thể xóa TwoFactorUserId để tránh conflict
                TempData.Remove("TwoFactorUserId");
                
                return RedirectToAction(nameof(VerifyLoginOtp));
            }
            catch (Exception ex)
            {
                TempData["TwoFactorError"] = "Có lỗi xảy ra khi gửi mã OTP: " + ex.Message;
                return RedirectToAction(nameof(VerifyAuthenticatorCode));
            }
        }

        [HttpGet("verify-login-otp")]
        [AllowAnonymous]
        public IActionResult VerifyLoginOtp()
        {
            if (TempData["LoginUserId"] == null)
            {
                return RedirectToAction(nameof(Login));
            }

            TempData.Keep("LoginUserId");
            TempData.Keep("LoginUserEmail");
            ViewBag.Email = TempData["LoginUserEmail"];
            ViewBag.Info = TempData["InfoMessage"];
            ViewBag.Error = TempData["ErrorMessage"];

            return View();
        }

        [HttpPost("verify-login-otp")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyLoginOtp(string otpCode)
        {
            var userIdValue = TempData.Peek("LoginUserId")?.ToString();
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out int userId))
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Xác thực OTP
            if (!await _twoFactorService.ValidateCodeAsync(userId, OtpPurpose.Login, otpCode))
            {
                ViewBag.Error = "Mã OTP không hợp lệ hoặc đã hết hạn. Vui lòng thử lại.";
                ViewBag.Email = user.Email;
                return View();
            }

            // Đăng nhập thành công
            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData.Remove("LoginUserId");
            TempData.Remove("LoginUserEmail");
            TempData["SuccessMessage"] = "Đăng nhập thành công!";

            return RedirectToAction(nameof(Profile), new { id = user.Id });
        }

        [HttpPost("resend-login-otp")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendLoginOtp()
        {
            var userIdValue = TempData["LoginUserId"]?.ToString();
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out int userId))
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Hủy các mã cũ
            await _twoFactorService.InvalidateCodesAsync(userId, OtpPurpose.Login);

            // Gửi mã mới
            await _twoFactorService.GenerateAndSendAsync(user, OtpPurpose.Login);

            TempData["LoginUserId"] = userId;
            TempData["LoginUserEmail"] = user.Email;
            TempData["InfoMessage"] = "Mã OTP mới đã được gửi đến email của bạn.";

            return RedirectToAction(nameof(VerifyLoginOtp));
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
        public async Task<IActionResult> EnableTwoFactor(string verificationCode)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(user.AuthenticatorKey))
            {
                TempData["StatusMessage"] = "error:Vui lòng tạo mã QR trước khi kích hoạt 2FA.";
                return RedirectToAction(nameof(SecuritySettings));
            }

            if (!_authenticatorService.ValidateTwoFactorCode(user.AuthenticatorKey, verificationCode))
            {
                TempData["StatusMessage"] = "error:Mã xác thực không hợp lệ. Vui lòng thử lại.";
                return RedirectToAction(nameof(SecuritySettings), new { generateKey = false });
            }

            user.TwoFactorEnabled = true;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["StatusMessage"] = "error:Không thể kích hoạt 2FA. Vui lòng thử lại.";
                return RedirectToAction(nameof(SecuritySettings));
            }

            TempData["StatusMessage"] = "success:Đã bật xác thực hai yếu tố thành công!";
            return RedirectToAction(nameof(SecuritySettings));
        }

        [HttpPost("twofactor/disable")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactor()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.TwoFactorEnabled = false;
            user.AuthenticatorKey = null;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["StatusMessage"] = "error:Không thể tắt 2FA. Vui lòng thử lại.";
                return RedirectToAction(nameof(SecuritySettings));
            }

            TempData["StatusMessage"] = "success:Đã tắt xác thực hai yếu tố.";
            return RedirectToAction(nameof(SecuritySettings));
        }

        [HttpGet("settings/password")]
        [Authorize]
        public async Task<IActionResult> PasswordSettings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ChangePasswordViewModel
            {
                StatusMessage = TempData["StatusMessage"] as string,
                TwoFactorEnabled = user.TwoFactorEnabled
            };

            return View("SettingsPassword", model);
        }

        [HttpPost("password/request-otp")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestChangePasswordOtp()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            try
            {
                await _twoFactorService.GenerateAndSendAsync(user, OtpPurpose.ChangePassword);
                return Json(new { success = true, message = "Mã OTP đổi mật khẩu đã được gửi đến email của bạn." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi OTP. Vui lòng thử lại." });
            }
        }


        [HttpPost("password/change")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["StatusMessage"] = "Thông tin đổi mật khẩu chưa hợp lệ.";
                return RedirectToAction(nameof(PasswordSettings));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (!await _userManager.CheckPasswordAsync(user, model.OldPassword))
            {
                TempData["StatusMessage"] = "Mật khẩu cũ không đúng.";
                return RedirectToAction(nameof(PasswordSettings));
            }

            if (user.TwoFactorEnabled)
            {
                var valid = await _twoFactorService.ValidateCodeAsync(user.Id, OtpPurpose.ChangePassword, model.OtpCode);
                if (!valid)
                {
                    TempData["StatusMessage"] = "Mã OTP đổi mật khẩu không hợp lệ.";
                    return RedirectToAction(nameof(PasswordSettings));
                }
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            TempData["StatusMessage"] = result.Succeeded
                ? "Đổi mật khẩu thành công."
                : string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(PasswordSettings));
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        [HttpPost("external-login")]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Users", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet("external-login-callback")]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                TempData["Error"] = $"Lỗi từ provider: {remoteError}";
                return RedirectToAction(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["Error"] = "Không thể lấy thông tin đăng nhập từ provider.";
                return RedirectToAction(nameof(Login));
            }

            // Try to sign in with existing external login
            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, 
                info.ProviderKey, 
                isPersistent: false, 
                bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }

            // If user doesn't exist, create one
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? info.Principal.FindFirstValue(ClaimTypes.GivenName);
            
            if (string.IsNullOrEmpty(email))
            {
                // Try to get email from GitHub avatar claim
                email = $"{info.ProviderKey}@{info.LoginProvider.ToLower()}.local";
            }

            // Check if user with this email exists
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                // Link the external login to existing user
                var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(existingUser, isPersistent: false);
                    TempData["SuccessMessage"] = $"Đã liên kết tài khoản {info.LoginProvider} với tài khoản của bạn.";
                    return LocalRedirect(returnUrl);
                }
            }

            // Create new user
            // Create new user
            // Sanitize username: Remove accents, keep only alphanumeric
            var rawName = name ?? "user";
            var normalized = rawName.Normalize(System.Text.NormalizationForm.FormD);
            var chars = normalized.Where(c => char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray();
            var asciiName = new string(chars).Normalize(System.Text.NormalizationForm.FormC);
            var safeName = System.Text.RegularExpressions.Regex.Replace(asciiName, "[^a-zA-Z0-9]", "");
            
            if (string.IsNullOrEmpty(safeName)) safeName = $"user{Guid.NewGuid().ToString("N")[..4]}";

            var userName = safeName;
            
            // Ensure unique username
            var baseUserName = userName;
            var counter = 1;
            while (await _userManager.FindByNameAsync(userName) != null)
            {
                userName = $"{baseUserName}{counter++}";
            }

            var newUser = new User
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true, // Trust the provider
                CreatedAt = DateTime.UtcNow
            };

            // Get avatar from provider
            var avatarClaim = info.Principal.FindFirstValue("urn:github:avatar") 
                ?? info.Principal.FindFirstValue("picture");
            if (!string.IsNullOrEmpty(avatarClaim))
            {
                newUser.AvatarUrl = avatarClaim;
            }

            var createResult = await _userManager.CreateAsync(newUser);
            if (createResult.Succeeded)
            {
                await _userManager.AddLoginAsync(newUser, info);
                await _signInManager.SignInAsync(newUser, isPersistent: false);
                
                TempData["SuccessMessage"] = $"Chào mừng {newUser.UserName}! Đăng ký thành công qua {info.LoginProvider}.";
                TempData["Celebration"] = true;
                return LocalRedirect(returnUrl);
            }

            var errorMsg = string.Join("; ", createResult.Errors.Select(e => e.Description));
            TempData["Error"] = $"Không thể tạo tài khoản: {errorMsg}";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet("api/external-providers")]
        public IActionResult GetExternalProviders()
        {
            var schemes = _signInManager.GetExternalAuthenticationSchemesAsync().Result;
            var providers = schemes.Select(s => new { s.Name, s.DisplayName }).ToList();
            return Json(providers);
        }
    }
}