using System;
using VzOverFlow.Models;

namespace VzOverFlow.Models.ViewModels
{
    public class UserProfileViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        public int QuestionCount { get; set; }
        public int AnswerCount { get; set; }
        public int Reputation { get; set; }
        public int ExperiencePoints { get; set; }
        public int VoteCount { get; set; }
        public int AcceptedAnswerCount { get; set; }
        public int QuestionVotesReceived { get; set; }
        public int AnswerVotesReceived { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool IsFollowing { get; set; }
        public DailyMission? TodayMission { get; set; }
        public List<BadgeType> Badges { get; set; } = new List<BadgeType>();
        public List<QuestionListItemViewModel> Questions { get; set; } = new List<QuestionListItemViewModel>();
        public List<AnswerListItemViewModel> Answers { get; set; } = new List<AnswerListItemViewModel>();
    }
}