using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models
{
    public class Vote
    {
        public int Id { get; set; }

        [Range(-1, 1)]
        public int Value { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = default!;

        public int? QuestionId { get; set; }
        public Question? Question { get; set; }

        public int? AnswerId { get; set; }
        public Answer? Answer { get; set; }
    }
}
