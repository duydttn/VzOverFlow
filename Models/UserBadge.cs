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
        // Question badges
    Curious = 1,       // ??t câu h?i ??u tiên
     Inquisitive = 2,   // ??t 5 câu h?i
        Questioner = 3,           // ??t 50 câu h?i
        
  // Answer badges
        Teacher = 10,          // Tr? l?i ??u tiên
 Educator = 11,// Tr? l?i 10 câu h?i
   Scholar = 12,      // Tr? l?i 50 câu h?i
  
  // Vote badges
        Supporter = 20,   // Vote 10 l?n
        Critic = 21,// Vote 100 l?n
Judge = 22,      // Vote 500 l?n
        
        // Reputation badges
  NiceAnswer = 30,       // Câu tr? l?i ??t 10 upvotes
     GoodAnswer = 31,          // Câu tr? l?i ??t 25 upvotes
        GreatAnswer = 32,         // Câu tr? l?i ??t 50 upvotes
        
        // Community badges
   Enthusiast = 40,          // ??ng nh?p 7 ngày liên ti?p
        Dedicated = 41, // ??ng nh?p 30 ngày liên ti?p
        Fanatic = 42,      // ??ng nh?p 100 ngày liên ti?p
        
        // Special badges
Popular = 50,        // Câu h?i ??t 100 views
        Famous = 51,      // Câu h?i ??t 1000 views
     Accepted = 52     // Có câu tr? l?i ???c ch?p nh?n
    }
}
