using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using VzOverFlow.Data;
using VzOverFlow.Models;

namespace VzOverFlow.Services
{
    public class VoteService : IVoteService
    {
        private readonly AppDbContext _context;
        private readonly IReputationService _reputationService;
        private readonly IGamificationService _gamificationService;

        public VoteService(
            AppDbContext context, 
            IReputationService reputationService,
            IGamificationService gamificationService)
        {
            _context = context;
            _reputationService = reputationService;
            _gamificationService = gamificationService;
        }

        public async Task<int> VoteQuestionAsync(int questionId, int userId, int value)
        {
            if (value < -1 || value > 1)
            {
                throw new ArgumentException("Vote value must be -1, 0, or 1", nameof(value));
            }

            var question = await _context.Questions.FindAsync(questionId);
            if (question == null) throw new ArgumentException("Question not found");

            var existingVote = await _context.Votes
                .FirstOrDefaultAsync(v => v.QuestionId == questionId && v.UserId == userId);

            int oldVoteValue = existingVote?.Value ?? 0;

            if (value == 0)
            {
                if (existingVote != null)
                {
                    _context.Votes.Remove(existingVote);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                if (existingVote != null)
                {
                    existingVote.Value = value;
                }
                else
                {
                    var vote = new Vote
                    {
                        QuestionId = questionId,
                        UserId = userId,
                        Value = value
                    };
                    _context.Votes.Add(vote);
                }
                await _context.SaveChangesAsync();

                // HOOK: Award XP for voting
                var voteType = value > 0 ? ActivityType.VoteUp : ActivityType.VoteDown;
                await _gamificationService.AwardXpAsync(userId, voteType, questionId: questionId);
            }

            // Update Reputation (only for others' content)
            if (question.UserId != userId)
            {
                await _reputationService.UpdateForVoteAsync(question.UserId, oldVoteValue, value);
            }

            return await GetQuestionVoteScoreAsync(questionId);
        }

        public async Task<int> VoteAnswerAsync(int answerId, int userId, int value)
        {
            if (value < -1 || value > 1)
            {
                throw new ArgumentException("Vote value must be -1, 0, or 1", nameof(value));
            }

            var answer = await _context.Answers.FindAsync(answerId);
            if (answer == null) throw new ArgumentException("Answer not found");

            var existingVote = await _context.Votes
                .FirstOrDefaultAsync(v => v.AnswerId == answerId && v.UserId == userId);

            int oldVoteValue = existingVote?.Value ?? 0;

            if (value == 0)
            {
                if (existingVote != null)
                {
                    _context.Votes.Remove(existingVote);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                if (existingVote != null)
                {
                    existingVote.Value = value;
                }
                else
                {
                    var vote = new Vote
                    {
                        AnswerId = answerId,
                        UserId = userId,
                        Value = value
                    };
                    _context.Votes.Add(vote);
                }
                await _context.SaveChangesAsync();

                // HOOK: Award XP for voting
                var voteType = value > 0 ? ActivityType.VoteUp : ActivityType.VoteDown;
                await _gamificationService.AwardXpAsync(userId, voteType, answerId: answerId);
            }

            // Update Reputation
            if (answer.UserId != userId)
            {
                await _reputationService.UpdateForVoteAsync(answer.UserId, oldVoteValue, value);
            }

            return await GetAnswerVoteScoreAsync(answerId);
        }

        public async Task<int> GetQuestionVoteScoreAsync(int questionId)
        {
            return await _context.Votes
                .Where(v => v.QuestionId == questionId)
                .SumAsync(v => (int?)v.Value) ?? 0;
        }

        public async Task<int> GetAnswerVoteScoreAsync(int answerId)
        {
            return await _context.Votes
                .Where(v => v.AnswerId == answerId)
                .SumAsync(v => (int?)v.Value) ?? 0;
        }

        public async Task<Vote?> GetUserVoteOnQuestionAsync(int questionId, int userId)
        {
            return await _context.Votes
                .FirstOrDefaultAsync(v => v.QuestionId == questionId && v.UserId == userId);
        }

        public async Task<Vote?> GetUserVoteOnAnswerAsync(int answerId, int userId)
        {
            return await _context.Votes
                .FirstOrDefaultAsync(v => v.AnswerId == answerId && v.UserId == userId);
        }
    }
}