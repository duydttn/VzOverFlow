using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Models;
using VzOverFlow.Models.ViewModels;
using VzOverFlow.Services.AIService;

namespace VzOverFlow.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly AppDbContext _context;
        private readonly IGeminiService _geminiService;
        private readonly IGamificationService _gamificationService;
        private readonly IReputationService _reputationService;
        private readonly INotificationService _notificationService;

        public QuestionService(
            AppDbContext context, 
            IGeminiService geminiService,
            IGamificationService gamificationService,
            IReputationService reputationService,
            INotificationService notificationService)
        {
            _context = context;
            _geminiService = geminiService;
            _gamificationService = gamificationService;
            _reputationService = reputationService;
            _notificationService = notificationService;
        }

        public async Task<QuestionDetailViewModel?> GetQuestionDetailAsync(int id, bool increaseViewCount = false, string? sortBy = "accepted")
        {
            var question = await _context.Questions
                .Include(q => q.User)
                .Include(q => q.Tags)
                .Include(q => q.Votes)
                .Include(q => q.Comments.OrderBy(c => c.CreatedAt))
                    .ThenInclude(c => c.User)
                .Include(q => q.Answers)
                    .ThenInclude(a => a.User)
                .Include(q => q.Answers)
                    .ThenInclude(a => a.Votes)
                .Include(q => q.Answers)
                    .ThenInclude(a => a.Comments.OrderBy(c => c.CreatedAt))
                        .ThenInclude(c => c.User)
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
                AnswerCount = question.Answers.Count,
                SortBy = sortBy ?? "accepted"
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

            // Generate Embedding
            try 
            {
                var textToEmbed = $"{question.Title} {question.Body}";
                var embedding = await _geminiService.GetEmbeddingAsync(textToEmbed);
                if (embedding.Length > 0)
                {
                    question.EmbeddingJson = System.Text.Json.JsonSerializer.Serialize(embedding);
                }
            }
            catch 
            {
                // Silently fail embedding generation
            }

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            // HOOK: Award XP and Reputation
            await _gamificationService.AwardXpAsync(question.UserId, ActivityType.AskQuestion);
            await _reputationService.UpdateForPostQuestionAsync(question.UserId);

            return question;
        }

        public async Task UpdateQuestionAsync(Question question, List<string> tags)
        {
            var existing = await _context.Questions
                .Include(q => q.Tags)
                .FirstOrDefaultAsync(q => q.Id == question.Id);

            if (existing == null) throw new InvalidOperationException("Question not found");

            existing.Title = question.Title;
            existing.Body = question.Body;
            existing.UpdatedAt = DateTime.UtcNow;
            
            existing.Tags.Clear();
            var resolvedTags = await ResolveTagsAsync(tags);
            foreach (var tag in resolvedTags)
            {
                existing.Tags.Add(tag);
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteQuestionAsync(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null) return;

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
        }

        public async Task<Answer> AddAnswerAsync(int questionId, Answer answer)
        {
            var question = await _context.Questions.FindAsync(questionId);
            if (question == null) throw new InvalidOperationException("Question not found");

            answer.QuestionId = questionId;
            answer.CreatedAt = DateTime.UtcNow;
            _context.Answers.Add(answer);
            await _context.SaveChangesAsync();

            // HOOK: Award XP and Reputation
            await _gamificationService.AwardXpAsync(answer.UserId, ActivityType.AnswerQuestion, questionId, answer.Id);
            await _reputationService.UpdateForPostAnswerAsync(answer.UserId);

            // HOOK: Notify Question Owner
            if (answer.UserId != question.UserId)
            {
                var answerer = await _context.Users.FindAsync(answer.UserId);
                var answererName = answerer?.UserName ?? "Ai đó";
                await _notificationService.NotifyAnswerAsync(question.UserId, question.Title, questionId, answererName);
            }

            return answer;
        }

        public async Task<Answer?> GetAnswerByIdAsync(int id)
        {
            return await _context.Answers.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task UpdateAnswerAsync(Answer answer)
        {
            var existing = await _context.Answers.FindAsync(answer.Id);
            if (existing == null) throw new InvalidOperationException("Answer not found");

            existing.Body = answer.Body;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAnswerAsync(int id)
        {
            var answer = await _context.Answers.FindAsync(id);
            if (answer == null) return;

            _context.Answers.Remove(answer);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AcceptAnswerAsync(int answerId, int userId)
        {
            var answer = await _context.Answers
                .Include(a => a.Question)
                .FirstOrDefaultAsync(a => a.Id == answerId);

            if (answer == null) return false;

            // Only Question Owner can accept
            if (answer.Question.UserId != userId) return false;

            // Cannot accept own answer? (StackOverflow allows it, let's allow it)
            // But usually you accept OTHER's answer.
            // If already accepted, maybe toggle off?
            // "Toggle" behavior is better UX.

            bool isAccepting = !answer.IsAccepted;

            if (isAccepting)
            {
                // Unaccept all other answers for this question
                var otherAnswers = await _context.Answers
                    .Where(a => a.QuestionId == answer.QuestionId && a.IsAccepted)
                    .ToListAsync();
                
                foreach (var other in otherAnswers)
                {
                    other.IsAccepted = false;
                }

                answer.IsAccepted = true;

                // Hook: Notify Answer Owner
                if (answer.UserId != userId)
                {
                    await _notificationService.NotifyAcceptAnswerAsync(answer.UserId, answer.Question.Title, answer.QuestionId);
                    
                    // Gamification: Award XP to Answer Owner for Accepted Answer
                    await _gamificationService.AwardXpAsync(answer.UserId, ActivityType.AnswerAccepted, answer.QuestionId, answer.Id);
                    await _reputationService.UpdateForPostAnswerAsync(answer.UserId); // Re-calculate reputation
                }
            }
            else
            {
                answer.IsAccepted = false;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Question>> FindSimilarQuestionsAsync(string title, string body)
        {
            var allQuestions = await _context.Questions
                .Where(q => q.EmbeddingJson != null)
                .ToListAsync();

            if (!allQuestions.Any()) return new List<Question>();

            try 
            {
                var inputEmbedding = await _geminiService.GetEmbeddingAsync($"{title} {body}");
                if (inputEmbedding.Length == 0) return new List<Question>();

                var similar = allQuestions
                    .Select(q => new { 
                        Question = q, 
                        Similarity = CosineSimilarity(inputEmbedding, System.Text.Json.JsonSerializer.Deserialize<float[]>(q.EmbeddingJson!)!) 
                    })
                    .Where(x => x.Similarity > 0.8)
                    .OrderByDescending(x => x.Similarity)
                    .Take(5)
                    .Select(x => x.Question)
                    .ToList();

                return similar;
            }
            catch 
            {
                return new List<Question>();
            }
        }

        private static double CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            double dotProduct = 0, normA = 0, normB = 0;
            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += (double)vectorA[i] * vectorB[i];
                normA += (double)vectorA[i] * vectorA[i];
                normB += (double)vectorB[i] * vectorB[i];
            }
            if (normA == 0 || normB == 0) return 0;
            return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        private async Task<List<Tag>> ResolveTagsAsync(IEnumerable<string> tags)
        {
            var normalized = tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Distinct()
                .Take(5)
                .ToList();

            if (!normalized.Any()) return new List<Tag>();

            var existing = await _context.Tags
                .Where(t => normalized.Contains(t.Name.ToLower()))
                .ToListAsync();

            var missing = normalized.Except(existing.Select(t => t.Name.ToLower()));
            foreach (var name in missing)
            {
                var originalName = normalized.First(n => n.ToLower() == name);
                existing.Add(new Tag { Name = originalName });
            }

            return existing;
        }
    }
}