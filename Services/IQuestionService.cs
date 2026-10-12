using System.Collections.Generic;
using System.Threading.Tasks;
using VzOverFlow.Models;
using VzOverFlow.Models.ViewModels;

namespace VzOverFlow.Services
{
    public interface IQuestionService
    {
        Task<QuestionDetailViewModel?> GetQuestionDetailAsync(int id, bool increaseViewCount = false, string? sortBy = "accepted");
        Task<Question?> GetQuestionEntityAsync(int id);
        Task<Question> CreateQuestionAsync(Question question, List<string> tags);
        Task UpdateQuestionAsync(Question question, List<string> tags);
        Task<List<Question>> FindSimilarQuestionsAsync(string title, string body);
        Task DeleteQuestionAsync(int id);

        Task<Answer> AddAnswerAsync(int questionId, Answer answer);
        Task<Answer?> GetAnswerByIdAsync(int id);
        Task UpdateAnswerAsync(Answer answer);
        Task DeleteAnswerAsync(int id);
        Task<bool> AcceptAnswerAsync(int answerId, int userId);
    }
}