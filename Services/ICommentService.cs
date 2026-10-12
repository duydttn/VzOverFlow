using System.Threading.Tasks;
using VzOverFlow.Models;

namespace VzOverFlow.Services
{
    public interface ICommentService
    {
        Task<Comment?> AddCommentAsync(string body, int userId, int? questionId, int? answerId);
        Task<bool> DeleteCommentAsync(int commentId, int userId, bool isAdmin);
    }
}