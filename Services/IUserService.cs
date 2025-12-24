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
    }
}

