
using System.Threading.Tasks;
using VzOverFlow.Models;

namespace VzOverFlow.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(int userId, string message, string link);
        Task NotifyAnswerAsync(int questionOwnerId, string questionTitle, int questionId, string answererName);
        Task NotifyAcceptAnswerAsync(int answerOwnerId, string questionTitle, int questionId);
        Task NotifyFollowAsync(int followedUserId, string followerName, int followerId);
    }
}