using System;
using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models
{
    /// <summary>
    /// Track daily mission progress (resets every day)
    /// </summary>
 public class DailyMission
    {
public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = default!;

        public DateTime Date { get; set; } // Date only (no time)

        // Mission counters
        public int QuestionsAsked { get; set; } = 0;
  public int AnswersGiven { get; set; } = 0;
  public int VotesCast { get; set; } = 0;

        // XP earned today
        public int TotalXpToday { get; set; } = 0;

      public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
