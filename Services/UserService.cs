using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Models;
using VzOverFlow.Models.ViewModels;

namespace VzOverFlow.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetUsersAsync(string? search = null)
        {
            var query = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    u.UserName.Contains(search) ||
                    (u.Email ?? string.Empty).Contains(search));
            }

            return await query
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<UserProfileViewModel?> GetUserProfileAsync(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Questions)
                    .ThenInclude(q => q.Votes)
                .Include(u => u.Answers)
                    .ThenInclude(a => a.Votes)
                .Include(u => u.Votes)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return null;
            }

            var questionVotesReceived = user.Questions.Sum(q => q.Votes.Sum(v => v.Value));
            var answerVotesReceived = user.Answers.Sum(a => a.Votes.Sum(v => v.Value));

            return new UserProfileViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email ?? string.Empty,
                CreatedAt = user.CreatedAt,
                QuestionCount = user.Questions.Count,
                AnswerCount = user.Answers.Count,
                Reputation = user.Reputation,
                VoteCount = user.Votes.Count,
                AcceptedAnswerCount = user.Answers.Count(a => a.IsAccepted),
                QuestionVotesReceived = questionVotesReceived,
                AnswerVotesReceived = answerVotesReceived,
                TwoFactorEnabled = user.TwoFactorEnabled
            };
        }

        public async Task<IEnumerable<UserProfileViewModel>> GetLeaderboardAsync(int take = 10)
        {
            return await _context.Users
                .AsNoTracking()
                .OrderByDescending(u => u.Reputation)
                .ThenBy(u => u.UserName)
                .Take(take)
                .Select(u => new UserProfileViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email ?? string.Empty,
                    CreatedAt = u.CreatedAt,
                    QuestionCount = u.Questions.Count,
                    AnswerCount = u.Answers.Count,
                    Reputation = u.Reputation,
                    TwoFactorEnabled = u.TwoFactorEnabled
                })
                .ToListAsync();
        }
    }
}
