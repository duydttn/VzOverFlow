using System.Collections.Generic;
using System.Threading.Tasks;
using VzOverFlow.Models;

namespace VzOverFlow.Services
{
    public interface IAiConfigService
    {
        Task<IEnumerable<AiConfig>> GetAllConfigsAsync();
        Task<AiConfig?> GetConfigByIdAsync(int id);
        Task<AiConfig?> GetActiveConfigByProviderAsync(string provider);
        Task<bool> AddConfigAsync(AiConfig config);
        Task<bool> UpdateConfigAsync(AiConfig config);
        Task<bool> DeleteConfigAsync(int id);
        Task<bool> ToggleConfigStatusAsync(int id);
        Task MarkUsedAsync(int id);
    }
}