using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Models;

namespace VzOverFlow.Services
{
    public class TwoFactorService : ITwoFactorService
    {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;

        public TwoFactorService(AppDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public async Task<string> GenerateAndSendAsync(User user, OtpPurpose purpose)
        {
            var code = GenerateCode();

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new InvalidOperationException("Người dùng chưa cấu hình email nên không thể gửi OTP.");
            }

            var otp = new OneTimeCode
            {
                UserId = user.Id,
                Code = code,
                Purpose = purpose,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            };

            _context.OneTimeCodes.Add(otp);
            await _context.SaveChangesAsync();

            var subject = purpose switch
            {
                OtpPurpose.EnableTwoFactor => "[VzOverFlow] Mã kích hoạt bảo mật 2 lớp",
                OtpPurpose.DisableTwoFactor => "[VzOverFlow] Mã vô hiệu hóa bảo mật 2 lớp",
                OtpPurpose.ChangePassword => "[VzOverFlow] Mã xác nhận đổi mật khẩu",
                OtpPurpose.EmailVerification => "[VzOverFlow] Xác thực email đăng ký",
                OtpPurpose.Login => "[VzOverFlow] Mã xác thực đăng nhập",
                _ => "[VzOverFlow] Mã xác thực"
            };

            var body = purpose == OtpPurpose.EmailVerification
    ? $@"
    <h2>Chào mừng đến với VzOverFlow!</h2>
                <p>Xin chào <strong>{user.UserName}</strong>,</p>
  <p>Cảm ơn bạn đã đăng ký tài khoản. Để hoàn tất quá trình đăng ký, vui lòng nhập mã OTP sau:</p>
       <div style='background-color: #f0f9ff; border-left: 4px solid #3b82f6; padding: 15px; margin: 20px 0;'>
      <p style='font-size: 24px; font-weight: bold; margin: 0; color: #1e40af;'>{code}</p>
       </div>
         <p>Mã sẽ hết hạn sau <strong>5 phút</strong>.</p>
<p>Nếu bạn không đăng ký tài khoản này, vui lòng bỏ qua email này.</p>
    <hr style='margin: 20px 0; border: none; border-top: 1px solid #e5e7eb;'>
     <p style='font-size: 12px; color: #6b7280;'>Email này được gửi tự động, vui lòng không trả lời.</p>"
    : purpose == OtpPurpose.Login
    ? $@"
    <h2>Xác thực đăng nhập VzOverFlow</h2>
  <p>Xin chào <strong>{user.UserName}</strong>,</p>
    <p>Chúng tôi nhận được yêu cầu đăng nhập vào tài khoản của bạn. Để tiếp tục, vui lòng nhập mã OTP sau:</p>
 <div style='background-color: #f0fdf4; border-left: 4px solid #10b981; padding: 15px; margin: 20px 0;'>
   <p style='font-size: 28px; font-weight: bold; margin: 0; color: #047857; letter-spacing: 4px;'>{code}</p>
 </div>
    <p>Mã sẽ hết hạn sau <strong>5 phút</strong>.</p>
    <p style='color: #dc2626;'><strong>⚠️ Cảnh báo:</strong> Nếu bạn không thực hiện đăng nhập này, vui lòng bỏ qua email và xem xét đổi mật khẩu ngay.</p>
       <hr style='margin: 20px 0; border: none; border-top: 1px solid #e5e7eb;'>
 <p style='font-size: 12px; color: #6b7280;'>Email này được gửi tự động từ VzOverFlow. Không chia sẻ mã này với bất kỳ ai.</p>"
    : $@"
    <p>Xin chào {user.UserName},</p>
          <p>Mã OTP của bạn là: <strong>{code}</strong></p>
        <p>Mã sẽ hết hạn sau 5 phút.</p>
     <p>Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>";

            await _emailSender.SendAsync(user.Email, subject, body);
            return code;
        }

        public async Task<bool> ValidateCodeAsync(int userId, OtpPurpose purpose, string code, bool markUsed = true)
        {
            var otp = await _context.OneTimeCodes
                .Where(o => o.UserId == userId && o.Purpose == purpose && !o.IsUsed && o.ExpiresAt >= DateTime.UtcNow)
                .OrderByDescending(o => o.ExpiresAt)
                .FirstOrDefaultAsync();

            if (otp == null || !string.Equals(otp.Code, code, StringComparison.Ordinal))
            {
                return false;
            }

            if (markUsed)
            {
                otp.IsUsed = true;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task InvalidateCodesAsync(int userId, OtpPurpose purpose)
        {
            var codes = await _context.OneTimeCodes
                .Where(o => o.UserId == userId && o.Purpose == purpose && !o.IsUsed)
                .ToListAsync();

            foreach (var code in codes)
            {
                code.IsUsed = true;
            }

            await _context.SaveChangesAsync();
        }

        private static string GenerateCode()
        {
            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            var value = BitConverter.ToUInt32(bytes, 0) % 1000000;
            return value.ToString("D6");
        }
    }
}

