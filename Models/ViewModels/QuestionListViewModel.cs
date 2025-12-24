using System;
using System.Collections.Generic;

namespace VzOverFlow.Models.ViewModels
{
    public class QuestionListViewModel
    {
        public IEnumerable<QuestionListItemViewModel> Questions { get; set; } = new List<QuestionListItemViewModel>();
        public string? Search { get; set; }
        public string? Tag { get; set; }
        public string? Sort { get; set; }
    }

    public class QuestionListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string BodyPreview { get; set; } = string.Empty;
        public int VoteScore { get; set; }
        public int AnswerCount { get; set; }
        public int ViewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();

        public int UserReputation { get; set; }
    }
}