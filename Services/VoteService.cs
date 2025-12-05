using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Models;

namespace VzOverFlow.Services
{
    public class VoteService : IVoteService
    {
        private readonly AppDbContext _context;

        public VoteService(AppDbContext context)
        {
   _context = context;
        }

        public async Task<int> VoteQuestionAsync(int questionId, int userId, int value)
        {
      // Validate value is -1, 0, or 1
 if (value < -1 || value > 1)
     {
                throw new ArgumentException("Vote value must be -1, 0, or 1", nameof(value));
 }

  var existingVote = await _context.Votes
         .FirstOrDefaultAsync(v => v.QuestionId == questionId && v.UserId == userId);

      if (value == 0)
            {
     // Remove vote
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
   // Update existing vote
          existingVote.Value = value;
}
         else
      {
            // Create new vote
              var vote = new Vote
        {
        QuestionId = questionId,
    UserId = userId,
 Value = value
      };
      _context.Votes.Add(vote);
      }
        await _context.SaveChangesAsync();
    }

          return await GetQuestionVoteScoreAsync(questionId);
        }

        public async Task<int> VoteAnswerAsync(int answerId, int userId, int value)
        {
            // Validate value is -1, 0, or 1
         if (value < -1 || value > 1)
      {
        throw new ArgumentException("Vote value must be -1, 0, or 1", nameof(value));
     }

      var existingVote = await _context.Votes
      .FirstOrDefaultAsync(v => v.AnswerId == answerId && v.UserId == userId);

            if (value == 0)
            {
      // Remove vote
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
 // Update existing vote
      existingVote.Value = value;
     }
   else
        {
    // Create new vote
        var vote = new Vote
       {
   AnswerId = answerId,
          UserId = userId,
    Value = value
    };
       _context.Votes.Add(vote);
 }
        await _context.SaveChangesAsync();
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
