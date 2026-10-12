using System.Collections.Generic;
using System.Threading.Tasks;
using VzOverFlow.Models;
using VzOverFlow.Models.ViewModels;

namespace VzOverFlow.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetUsersAsync(string? search = null);
        Task<UserProfileViewModel?> GetUserProfileAsync(int userId, int currentUserId = 0);
        Task<IEnumerable<UserProfileViewModel>> GetLeaderboardAsync(int take = 10);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> ToggleUserBanAsync(int userId);
        Task<bool> AdjustReputationAsync(int userId, int amount);
        Task<bool> ChangeUserRoleAsync(int userId, string roleName);
        Task<AdminDashboardStatsViewModel> GetAdminDashboardStatsAsync();
    }
}