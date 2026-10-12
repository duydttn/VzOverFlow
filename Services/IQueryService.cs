using System.Threading.Tasks;
using VzOverFlow.Models.ViewModels;

namespace VzOverFlow.Services
{
    public interface IQueryService
    {
        Task<QuestionListViewModel> GetQuestionsAsync(string? search, string? tag, string? sort = null, int page = 1, int pageSize = 15);
    }
}