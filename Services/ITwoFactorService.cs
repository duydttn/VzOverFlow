using System.Threading.Tasks;
using VzOverFlow.Models;

namespace VzOverFlow.Services
{
    public interface ITwoFactorService
    {
        Task<string> GenerateAndSendAsync(User user, OtpPurpose purpose);
        Task<bool> ValidateCodeAsync(int userId, OtpPurpose purpose, string code, bool markUsed = true);
        Task InvalidateCodesAsync(int userId, OtpPurpose purpose);
    }
}

