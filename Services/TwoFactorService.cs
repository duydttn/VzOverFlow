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
                _ => "[VzOverFlow] Mã đăng nhập"
            };

            var body = $@"
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

