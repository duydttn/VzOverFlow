using System;
using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models
{
    public class UserActivity
    {
        public int Id { get; set; }

  [Required]
        public int UserId { get; set; }
        public User User { get; set; } = default!;

        [Required]
        public ActivityType Type { get; set; }

        public int? QuestionId { get; set; }
        public Question? Question { get; set; }

        public int? AnswerId { get; set; }
    public Answer? Answer { get; set; }

     public int XpEarned { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum ActivityType
    {
        AskQuestion = 1,
        AnswerQuestion = 2,
   VoteUp = 3,
     VoteDown = 4,
        AcceptAnswer = 5,
        QuestionUpvoted = 6,
        AnswerUpvoted = 7,
        AnswerAccepted = 8
    }
}
