using System.Threading.Tasks;

namespace VzOverFlow.Services
{
    public interface IReputationService
    {
        Task UpdateReputationAsync(int userId, int amount);
        Task UpdateForPostQuestionAsync(int userId);
        Task UpdateForPostAnswerAsync(int userId);
        Task UpdateForShareAsync(int userId);
        Task UpdateForVoteAsync(int targetUserId, int oldVoteValue, int newVoteValue);
        Task RecalculateAllUserReputationsAsync();
    }
}