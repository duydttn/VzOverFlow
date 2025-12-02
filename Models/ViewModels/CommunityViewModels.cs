using System.Collections.Generic;

namespace VzOverFlow.Models.ViewModels
{
    public class PopularTagViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
    }

    public class RightSidebarViewModel
    {
        public List<PopularTagViewModel> PopularTags { get; set; } = new();
        public int TotalQuestions { get; set; }
        public int TotalAnswers { get; set; }
    }

    public class BadgeStatusViewModel
    {
        public string Tier { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Target { get; set; }
        public int Progress { get; set; }
        public int RewardXp { get; set; }
        public bool Achieved => Progress >= Target;
        public double Completion => Target == 0 ? 1 : (double)Progress / Target;
    }

    public class BadgePageViewModel
    {
        public string? UserName { get; set; }
        public List<BadgeStatusViewModel> Badges { get; set; } = new();
        public bool RequiresLogin => UserName == null;
    }

    public class LearningPathViewModel
    {
        public string TagName { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public int AnswerCount { get; set; }
        public double Completion => QuestionCount == 0 ? 0 : AnswerCount / (double)QuestionCount;
    }
}

