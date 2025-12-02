using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string UserName { get; set; } = default!;

        [EmailAddress, StringLength(200)]
        public string? Email { get; set; }

        [Required]
        public string PasswordHash { get; set; } = default!;

        public int Reputation { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool TwoFactorEnabled { get; set; }

        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
        public ICollection<OneTimeCode> OneTimeCodes { get; set; } = new List<OneTimeCode>();
    }
}