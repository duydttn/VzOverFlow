using System.Collections.Generic;
using System.Threading.Tasks;
using VzOverFlow.Models;
using VzOverFlow.Models.ViewModels;

namespace VzOverFlow.Services
{
    public interface IQuestionService
    {
        Task<QuestionListViewModel> GetQuestionsAsync(string? search, string? tag, string? sort = null);
        Task<QuestionDetailViewModel?> GetQuestionDetailAsync(int id, bool increaseViewCount = false);
        Task<Question?> GetQuestionEntityAsync(int id);
        Task<Question> CreateQuestionAsync(Question question, List<string> tags);
        Task UpdateQuestionAsync(Question question, List<string> tags);
        Task DeleteQuestionAsync(int id);

        Task<Answer> AddAnswerAsync(int questionId, Answer answer);
        Task<Answer?> GetAnswerByIdAsync(int id);
        Task UpdateAnswerAsync(Answer answer);
        Task DeleteAnswerAsync(int id);

        Task<int> VoteQuestionAsync(int questionId, int userId, int value);
        Task<int> VoteAnswerAsync(int answerId, int userId, int value);
    }
}
