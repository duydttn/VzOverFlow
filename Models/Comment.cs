using System;
using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        public string Body { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
        public User User { get; set; } = default!;

        public int? QuestionId { get; set; }
        public Question? Question { get; set; }

        public int? AnswerId { get; set; }
        public Answer? Answer { get; set; }
    }
}
