using System;

namespace VzOverFlow.Models.ViewModels
{
    public class UserProfileViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public DateTime CreatedAt { get; set; }

        public int QuestionCount { get; set; }
        public int AnswerCount { get; set; }
        public int Reputation { get; set; }
        public int VoteCount { get; set; }
        public int AcceptedAnswerCount { get; set; }
        public int QuestionVotesReceived { get; set; }
        public int AnswerVotesReceived { get; set; }
        public bool TwoFactorEnabled { get; set; }
    }
}
