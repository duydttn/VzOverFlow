using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using VzOverFlow.Data;
using VzOverFlow.Models;

namespace VzOverFlow.Services
{
    public class ReputationService : IReputationService
    {
        private readonly AppDbContext _context;

        public ReputationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task UpdateReputationAsync(int userId, int amount)
        {
            if (amount == 0) return;

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Reputation += amount;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateForPostQuestionAsync(int userId)
        {
            await UpdateReputationAsync(userId, 2);
        }

        public async Task UpdateForPostAnswerAsync(int userId)
        {
            await UpdateReputationAsync(userId, 3);
        }

        public async Task UpdateForShareAsync(int userId)
        {
            await UpdateReputationAsync(userId, 1);
        }

        public async Task UpdateForVoteAsync(int targetUserId, int oldVoteValue, int newVoteValue)
        {
            // Logic:
            // Upvote (+1) received: +1 Rep
            // Downvote (-1) received: -1 Rep
            // Un-Upvote (1 -> 0): -1 Rep
            // Un-Downvote (-1 -> 0): +1 Rep
            // Up -> Down (1 -> -1): -2 Rep
            // Down -> Up (-1 -> 1): +2 Rep
            
            int delta = newVoteValue - oldVoteValue;
            // The score change maps 1:1 to the vote value change in this simple system
            // (Vote +1 => Rep +1, Vote -1 => Rep -1)
            
            await UpdateReputationAsync(targetUserId, delta);
        }

        public async Task RecalculateAllUserReputationsAsync()
        {
            var users = await _context.Users
                .Include(u => u.Questions)
                .Include(u => u.Answers)
                .ToListAsync();

            // We need to fetch all votes efficiently. 
            // Loading all votes in memory might be heavy. Let's do it per user or with a separate query.
            // For now, let's query aggregates.

            foreach (var user in users)
            {
                int newReputation = 0;

                // 1. Questions (+2 each)
                newReputation += user.Questions.Count * 2;

                // 2. Answers (+3 each)
                newReputation += user.Answers.Count * 3;

                // 3. Votes received on Questions
                var questionIds = user.Questions.Select(q => q.Id).ToList();
                if (questionIds.Any())
                {
                    var qVotes = await _context.Votes
                        .Where(v => v.QuestionId.HasValue && questionIds.Contains(v.QuestionId.Value))
                        .SumAsync(v => (int)v.Value);
                    newReputation += qVotes;
                }

                // 4. Votes received on Answers
                var answerIds = user.Answers.Select(a => a.Id).ToList();
                if (answerIds.Any())
                {
                    var aVotes = await _context.Votes
                        .Where(v => v.AnswerId.HasValue && answerIds.Contains(v.AnswerId.Value))
                        .SumAsync(v => (int)v.Value);
                    newReputation += aVotes;
                }

                // 5. Shares (+1 each)
                // Assuming we track shares in UserActivity with Type = Share
                var shareCount = await _context.UserActivities
                    .CountAsync(a => a.UserId == user.Id && a.Type == ActivityType.Share);
                newReputation += shareCount;

                // Ensure non-negative reputation if desired? Usually Rep >= 1.
                // StackOverflow starts at 1. Let's assume start at 1.
                // User.Reputation default is 1. So newReputation should be + calculated.
                // But if they have penalties, it could drop.
                // Let's set it to max(1, 1 + calculated).
                user.Reputation = Math.Max(1, 1 + newReputation);
            }

            await _context.SaveChangesAsync();
        }
    }
}