using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace VzOverFlow.Models
{
    public class User : IdentityUser<int>
    {
        public int Reputation { get; set; } = 1;
        public int ExperiencePoints { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public string? AuthenticatorKey { get; set; }

        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
        public ICollection<OneTimeCode> OneTimeCodes { get; set; } = new List<OneTimeCode>();
        public ICollection<UserActivity> Activities { get; set; } = new List<UserActivity>();
        public ICollection<UserBadge> Badges { get; set; } = new List<UserBadge>();
        public ICollection<DailyMission> DailyMissions { get; set; } = new List<DailyMission>();
        public ICollection<UserFollow> Following { get; set; } = new List<UserFollow>();
        public ICollection<UserFollow> Followers { get; set; } = new List<UserFollow>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}