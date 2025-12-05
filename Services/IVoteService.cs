using VzOverFlow.Models;

namespace VzOverFlow.Services
{
    public interface IVoteService
    {
        Task<int> VoteQuestionAsync(int questionId, int userId, int value);
        Task<int> VoteAnswerAsync(int answerId, int userId, int value);
    Task<int> GetQuestionVoteScoreAsync(int questionId);
        Task<int> GetAnswerVoteScoreAsync(int answerId);
        Task<Vote?> GetUserVoteOnQuestionAsync(int questionId, int userId);
        Task<Vote?> GetUserVoteOnAnswerAsync(int answerId, int userId);
    }
}
