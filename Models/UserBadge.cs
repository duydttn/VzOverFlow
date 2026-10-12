using System;
using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models
{
    public class UserBadge
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = default!;

        [Required]
        public BadgeType Badge { get; set; }

        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    }

    public enum BadgeType
    {
        Curious = 1,       // Đặt câu hỏi đầu tiên
        Inquisitive = 2,   // Đặt 5 câu hỏi
        Questioner = 3,    // Đặt 50 câu hỏi
        Teacher = 10,      // Trả lời đầu tiên
        Educator = 11,     // Trả lời 10 câu hỏi
        Scholar = 12,      // Trả lời 50 câu hỏi
        Supporter = 20,    // Vote 10 lần
        Critic = 21,       // Vote 100 lần
        Judge = 22,        // Vote 500 lần
        NiceAnswer = 30,   // Câu trả lời đạt 10 upvotes
        GoodAnswer = 31,   // Câu trả lời đạt 25 upvotes
        GreatAnswer = 32,  // Câu trả lời đạt 50 upvotes
        Enthusiast = 40,   // Đăng nhập 7 ngày liên tiếp
        Dedicated = 41,    // Đăng nhập 30 ngày liên tiếp
        Fanatic = 42,      // Đăng nhập 100 ngày liên tiếp
        Popular = 50,      // Câu hỏi đạt 100 views
        Famous = 51,       // Câu hỏi đạt 1000 views
        Accepted = 52      // Có câu trả lời được chấp nhận
    }
}