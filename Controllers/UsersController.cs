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
        private readonly IAuthenticatorService _authenticatorService;

        public UsersController(
            IUserService userService,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ITwoFactorService twoFactorService,
            IAuthenticatorService authenticatorService)
        {
            _userService = userService;
            _userManager = userManager;
            _signInManager = signInManager;
            _twoFactorService = twoFactorService;
            _authenticatorService = authenticatorService;
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
        public async Task<IActionResult> SecuritySettings(bool generateKey = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Generate new authenticator key if requested and 2FA is not enabled
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

            // Only show QR code and key if 2FA is NOT enabled yet (setup mode)
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
                EmailConfirmed = false // Email chưa được xác thực
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            // Gửi OTP để xác thực email
            await _twoFactorService.GenerateAndSendAsync(user, OtpPurpose.EmailVerification);
     
            // Lưu UserId vào TempData để verify
            TempData["PendingUserId"] = user.Id;
            TempData["PendingUserEmail"] = user.Email;
            TempData["InfoMessage"] = "Một mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra và nhập mã để hoàn tất đăng ký.";
          
            return RedirectToAction(nameof(VerifyEmail));
        }

        [HttpGet("verify-email")]
  [AllowAnonymous]
     public IActionResult VerifyEmail()
        {
     if (TempData["PendingUserId"] == null)
{
           return RedirectToAction(nameof(Register));
        }

            TempData.Keep("PendingUserId");
            TempData.Keep("PendingUserEmail");
            ViewBag.Email = TempData["PendingUserEmail"];
            ViewBag.Info = TempData["InfoMessage"];
   ViewBag.Error = TempData["ErrorMessage"];
    
            return View();
        }

[HttpPost("verify-email")]
  [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(string otpCode)
        {
      var userIdValue = TempData["PendingUserId"]?.ToString();
if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out int userId))
        {
         return RedirectToAction(nameof(Register));
     }

       var user = await _userManager.FindByIdAsync(userId.ToString());
      if (user == null)
 {
      return RedirectToAction(nameof(Register));
            }

        // Validate OTP
            if (!await _twoFactorService.ValidateCodeAsync(userId, OtpPurpose.EmailVerification, otpCode))
            {
  TempData["ErrorMessage"] = "Mã OTP không hợp lệ hoặc đã hết hạn. Vui lòng thử lại.";
    TempData["PendingUserId"] = userId;
         TempData["PendingUserEmail"] = user.Email;
   return RedirectToAction(nameof(VerifyEmail));
   }

            // Xác thực email thành công
            user.EmailConfirmed = true;
     await _userManager.UpdateAsync(user);

     // Đăng nhập người dùng
          await _signInManager.SignInAsync(user, isPersistent: false);
    
  TempData.Remove("PendingUserId");
   TempData.Remove("PendingUserEmail");
 TempData["WelcomeMessage"] = $"Chào mừng {user.UserName}! Đăng ký thành công. Email của bạn đã được xác thực.";
       
     return RedirectToAction("Index", "Home");
      }

        [HttpPost("resend-email-verification")]
  [AllowAnonymous]
  [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailVerification()
   {
       var userIdValue = TempData["PendingUserId"]?.ToString();
     if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out int userId))
       {
  return RedirectToAction(nameof(Register));
     }

  var user = await _userManager.FindByIdAsync(userId.ToString());
       if (user == null)
{
     return RedirectToAction(nameof(Register));
      }

   // Hủy các mã cũ
 await _twoFactorService.InvalidateCodesAsync(userId, OtpPurpose.EmailVerification);

    // Gửi mã mới
 await _twoFactorService.GenerateAndSendAsync(user, OtpPurpose.EmailVerification);

   TempData["PendingUserId"] = userId;
            TempData["PendingUserEmail"] = user.Email;
    TempData["InfoMessage"] = "Mã OTP mới đã được gửi đến email của bạn.";

 return RedirectToAction(nameof(VerifyEmail));
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

   // Kiểm tra user có email không
 if (string.IsNullOrEmpty(user.Email))
      {
        ViewBag.Error = "Tài khoản chưa có email. Vui lòng liên hệ quản trị viên.";
          return View(model);
          }

         // Nếu đã bật 2FA với Authenticator App
       if (user.TwoFactorEnabled && !string.IsNullOrEmpty(user.AuthenticatorKey))
  {
       TempData["TwoFactorUserId"] = user.Id;
      TempData["InfoMessage"] = "Vui lòng nhập mã từ ứng dụng xác thực của bạn.";
     return RedirectToAction(nameof(VerifyAuthenticatorCode));
       }

          // Với tất cả trường hợp khác, gửi OTP qua email
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
     var userIdValue = TempData["TwoFactorUserId"]?.ToString();
   if (string.IsNullOrEmpty(userIdValue))
      {
      return RedirectToAction(nameof(Login));
            }

      var user = await _userManager.FindByIdAsync(userIdValue);
   if (user == null || string.IsNullOrEmpty(user.AuthenticatorKey))
   {
                return RedirectToAction(nameof(Login));
     }

     if (!_authenticatorService.ValidateTwoFactorCode(user.AuthenticatorKey, code))
      {
       TempData["TwoFactorError"] = "Mã xác thực không hợp lệ. Vui lòng thử lại.";
            TempData.Keep("TwoFactorUserId");
 return RedirectToAction(nameof(VerifyAuthenticatorCode));
         }

 await _signInManager.SignInAsync(user, isPersistent: false);
TempData.Remove("TwoFactorUserId");
  return RedirectToAction(nameof(Profile), new { id = user.Id });
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

      // Validate OTP
     if (!await _twoFactorService.ValidateCodeAsync(userId, OtpPurpose.Login, otpCode))
    {
        TempData["ErrorMessage"] = "Mã OTP không hợp lệ hoặc đã hết hạn. Vui lòng thử lại.";
   TempData["LoginUserId"] = userId;
       TempData["LoginUserEmail"] = user.Email;
                return RedirectToAction(nameof(VerifyLoginOtp));
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
