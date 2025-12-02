using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models
{
    public class Question
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [StringLength(200, MinimumLength = 15, ErrorMessage = "Tiêu đề phải dài 15-200 ký tự")]
        public string Title { get; set; } = default!;

        [Required(ErrorMessage = "Nội dung là bắt buộc")]
        public string Body { get; set; } = default!;

        public int ViewCount { get; set; }
        public bool IsClosed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = default!;

        public int? AcceptedAnswerId { get; set; }
        public Answer? AcceptedAnswer { get; set; }

        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    }
}
