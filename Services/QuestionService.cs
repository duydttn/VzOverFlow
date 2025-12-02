using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Models;
using VzOverFlow.Models.ViewModels;

namespace VzOverFlow.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly AppDbContext _context;

        public QuestionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<QuestionListViewModel> GetQuestionsAsync(string? search, string? tag, string? sort = null)
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
                "votes" => query.OrderByDescending(q => q.Votes.Sum(v => (int?)v.Value) ?? 0),
                "unanswered" => query.OrderBy(q => q.Answers.Count).ThenByDescending(q => q.CreatedAt),
                "active" => query.OrderByDescending(q => q.UpdatedAt ?? q.CreatedAt),
                _ => query.OrderByDescending(q => q.CreatedAt)
            };

            var items = await query
                .Select(q => new QuestionListItemViewModel
                {
                    Id = q.Id,
                    Title = q.Title,
                    AnswerCount = q.Answers.Count,
                    VoteScore = q.Votes.Sum(v => (int?)v.Value) ?? 0,
                    ViewCount = q.ViewCount,
                    UserName = q.User.UserName,
                    CreatedAt = q.CreatedAt,
                    Tags = q.Tags.Select(t => t.Name).ToList()
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

        public async Task<QuestionDetailViewModel?> GetQuestionDetailAsync(int id, bool increaseViewCount = false)
        {
            var question = await _context.Questions
                .Include(q => q.User)
                .Include(q => q.Tags)
                .Include(q => q.Votes)
                .Include(q => q.Answers)
                    .ThenInclude(a => a.User)
                .Include(q => q.Answers)
                    .ThenInclude(a => a.Votes)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                return null;
            }

            if (increaseViewCount)
            {
                question.ViewCount += 1;
                await _context.SaveChangesAsync();
            }

            var viewModel = new QuestionDetailViewModel
            {
                Question = question,
                VoteScore = question.Votes.Sum(v => v.Value),
                AnswerCount = question.Answers.Count
            };

            return viewModel;
        }

        public async Task<Question?> GetQuestionEntityAsync(int id)
        {
            return await _context.Questions
                .Include(q => q.Tags)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<Question> CreateQuestionAsync(Question question, List<string> tags)
        {
            question.Tags = await ResolveTagsAsync(tags);
            question.CreatedAt = DateTime.UtcNow;
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
            return question;
        }

        public async Task UpdateQuestionAsync(Question question, List<string> tags)
        {
            var existing = await _context.Questions
                .Include(q => q.Tags)
                .FirstOrDefaultAsync(q => q.Id == question.Id);

            if (existing == null)
            {
                throw new InvalidOperationException("Question not found");
            }

            existing.Title = question.Title;
            existing.Body = question.Body;
            existing.UpdatedAt = DateTime.UtcNow;

            existing.Tags.Clear();
            var resolved = await ResolveTagsAsync(tags);
            foreach (var tag in resolved)
            {
                existing.Tags.Add(tag);
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteQuestionAsync(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null)
            {
                return;
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
        }

        public async Task<Answer> AddAnswerAsync(int questionId, Answer answer)
        {
            var question = await _context.Questions.FindAsync(questionId);
            if (question == null)
            {
                throw new InvalidOperationException("Question not found");
            }

            answer.QuestionId = questionId;
            answer.CreatedAt = DateTime.UtcNow;
            _context.Answers.Add(answer);
            await _context.SaveChangesAsync();
            return answer;
        }

        public async Task<Answer?> GetAnswerByIdAsync(int id)
        {
            return await _context.Answers.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task UpdateAnswerAsync(Answer answer)
        {
            var existing = await _context.Answers.FindAsync(answer.Id);
            if (existing == null)
            {
                throw new InvalidOperationException("Answer not found");
            }

            existing.Body = answer.Body;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAnswerAsync(int id)
        {
            var answer = await _context.Answers.FindAsync(id);
            if (answer == null)
            {
                return;
            }

            _context.Answers.Remove(answer);
            await _context.SaveChangesAsync();
        }

        public async Task<int> VoteQuestionAsync(int questionId, int userId, int value)
        {
            return await UpsertVoteAsync(userId, value, questionId, null);
        }

        public async Task<int> VoteAnswerAsync(int answerId, int userId, int value)
        {
            return await UpsertVoteAsync(userId, value, null, answerId);
        }

        private async Task<int> UpsertVoteAsync(int userId, int value, int? questionId, int? answerId)
        {
            value = Math.Clamp(value, -1, 1);

            var vote = await _context.Votes.FirstOrDefaultAsync(v =>
                v.UserId == userId &&
                v.QuestionId == questionId &&
                v.AnswerId == answerId);

            if (value == 0)
            {
                if (vote != null)
                {
                    _context.Votes.Remove(vote);
                    await _context.SaveChangesAsync();
                }

                return await SumVotesAsync(questionId, answerId);
            }

            if (vote == null)
            {
                vote = new Vote
                {
                    UserId = userId,
                    QuestionId = questionId,
                    AnswerId = answerId,
                    Value = value
                };
                _context.Votes.Add(vote);
            }
            else
            {
                vote.Value = value;
            }

            await _context.SaveChangesAsync();

            return await SumVotesAsync(questionId, answerId);
        }

        private async Task<List<Tag>> ResolveTagsAsync(IEnumerable<string> tags)
        {
            var normalized = tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Distinct()
                .Take(5)
                .ToList();

            if (!normalized.Any())
            {
                return new List<Tag>();
            }

            var existing = await _context.Tags
                .Where(t => normalized.Contains(t.Name.ToLower()))
                .ToListAsync();

            var missing = normalized.Except(existing.Select(t => t.Name));
            foreach (var name in missing)
            {
                existing.Add(new Tag { Name = name });
            }

            return existing;
        }

        private async Task<int> SumVotesAsync(int? questionId, int? answerId)
        {
            if (questionId.HasValue)
            {
                return await _context.Votes
                    .Where(v => v.QuestionId == questionId)
                    .SumAsync(v => (int?)v.Value) ?? 0;
            }

            return await _context.Votes
                .Where(v => v.AnswerId == answerId)
                .SumAsync(v => (int?)v.Value) ?? 0;
        }
    }
}

