using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Models.ViewModels;

namespace VzOverFlow.Services
{
    public class QueryService : IQueryService
    {
        private readonly AppDbContext _context;

        public QueryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<QuestionListViewModel> GetQuestionsAsync(string? search, string? tag, string? sort = null, int page = 1, int pageSize = 15)
        {
            var query = _context.Questions
                .AsNoTracking()
                .Include(q => q.User)
                .Include(q => q.Answers)
                .Include(q => q.Votes)
                .Include(q => q.Tags)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(q => q.Title.Contains(search) || q.Body.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                query = query.Where(q => q.Tags.Any(t => t.Name == tag));
            }

            query = sort switch
            {
                "trending" => query.OrderByDescending(q => (q.Votes.Sum(v => (int?)v.Value) ?? 0) * 10 + q.ViewCount + q.Answers.Count * 5),
                "votes" => query.OrderByDescending(q => q.Votes.Sum(v => (int?)v.Value) ?? 0),
                "unanswered" => query.Where(q => q.Answers.Count == 0).OrderByDescending(q => q.CreatedAt),
                "active" => query.OrderByDescending(q => q.UpdatedAt ?? q.CreatedAt),
                _ => query.OrderByDescending(q => q.CreatedAt)
            };

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(q => new QuestionListItemViewModel
                {
                    Id = q.Id,
                    Title = q.Title,
                    AnswerCount = q.Answers.Count,
                    BodyPreview = q.Body.Length > 200 ? q.Body.Substring(0, 200) + "..." : q.Body,
                    VoteScore = q.Votes.Sum(v => (int?)v.Value) ?? 0,
                    ViewCount = q.ViewCount,
                    UserName = q.User.UserName,
                    CreatedAt = q.CreatedAt,
                    Tags = q.Tags.Select(t => t.Name).ToList(),
                    UserReputation = q.User.Reputation,
                    UserId = q.User.Id,
                    AvatarUrl = q.User.AvatarUrl
                })
                .ToListAsync();

            return new QuestionListViewModel
            {
                Questions = items,
                Search = search,
                Tag = tag,
                Sort = sort
            };
        }
    }
}