using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Models;

namespace VzOverFlow.Services
{
    public class AiConfigService : IAiConfigService
    {
        private readonly AppDbContext _context;

        public AiConfigService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AiConfig>> GetAllConfigsAsync()
        {
            return await _context.AiConfigs.OrderByDescending(c => c.CreatedAt).ToListAsync();
        }

        public async Task<AiConfig?> GetConfigByIdAsync(int id)
        {
            return await _context.AiConfigs.FindAsync(id);
        }

        public async Task<AiConfig?> GetActiveConfigByProviderAsync(string provider)
        {
            // Simple rotation/pool logic: pick the one least used among active ones
            return await _context.AiConfigs
                .Where(c => c.Provider == provider && c.IsActive)
                .OrderBy(c => c.LastUsedAt ?? DateTime.MinValue)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> AddConfigAsync(AiConfig config)
        {
            config.CreatedAt = DateTime.UtcNow;
            _context.AiConfigs.Add(config);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateConfigAsync(AiConfig config)
        {
            _context.AiConfigs.Update(config);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteConfigAsync(int id)
        {
            var config = await _context.AiConfigs.FindAsync(id);
            if (config == null) return false;

            _context.AiConfigs.Remove(config);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ToggleConfigStatusAsync(int id)
        {
            var config = await _context.AiConfigs.FindAsync(id);
            if (config == null) return false;

            config.IsActive = !config.IsActive;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task MarkUsedAsync(int id)
        {
            var config = await _context.AiConfigs.FindAsync(id);
            if (config != null)
            {
                config.LastUsedAt = DateTime.UtcNow;
                config.UsageCount++;
                await _context.SaveChangesAsync();
            }
        }
    }
}