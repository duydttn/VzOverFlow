using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Models;

namespace VzOverFlow.Services
{
    public class CommentService : ICommentService
    {
        private readonly AppDbContext _context;

        public CommentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Comment?> AddCommentAsync(string body, int userId, int? questionId, int? answerId)
        {
            body = body.TrimStart();
            if (string.IsNullOrEmpty(body)) return null;

            var comment = new Comment
            {
                Body = body,
                UserId = userId,
                QuestionId = questionId,
                AnswerId = answerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            await _context.Entry(comment).Reference(c => c.User).LoadAsync();
            
            return comment;
        }

        public async Task<bool> DeleteCommentAsync(int commentId, int userId, bool isAdmin)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return false;

            if (comment.UserId != userId && !isAdmin) return false;

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}