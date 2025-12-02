using System;
using System.Collections.Generic;

namespace VzOverFlow.Models.ViewModels
{
    public class QuestionListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public int AnswerCount { get; set; }
        public int VoteScore { get; set; }
        public int ViewCount { get; set; }
        public string UserName { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class QuestionListViewModel
    {
        public List<QuestionListItemViewModel> Questions { get; set; } = new();
        public string? Search { get; set; }
        public string? Tag { get; set; }
        public string? Sort { get; set; }
    }
}
