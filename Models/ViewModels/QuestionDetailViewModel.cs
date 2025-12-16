using VzOverFlow.Models;

namespace VzOverFlow.Models.ViewModels
{
    public class QuestionDetailViewModel
    {
        public Question Question { get; set; } = default!;
        public int VoteScore { get; set; }
        public int AnswerCount { get; set; }
        public Answer? NewAnswer { get; set; }
        public string SortBy { get; set; } = "accepted";
    }
}
