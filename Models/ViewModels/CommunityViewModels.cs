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

    public class LearningPathViewModel
    {
        public string TagName { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public int AnswerCount { get; set; }
        public double Completion => QuestionCount == 0 ? 0 : AnswerCount / (double)QuestionCount;
    }
}