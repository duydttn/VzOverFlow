using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models
{
    public class Answer
    {
        public int Id { get; set; }

        [Required]
        public string Body { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsAccepted { get; set; }

        public int QuestionId { get; set; }
        public Question Question { get; set; } = default!;

        public int UserId { get; set; }
        public User User { get; set; } = default!;

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    }
}