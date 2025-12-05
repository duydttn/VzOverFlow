using System;
using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models
{
    public class Report
    {
     public int Id { get; set; }

        [Required]
        public int ReporterId { get; set; }
        public User Reporter { get; set; } = default!;

        public int? QuestionId { get; set; }
        public Question? Question { get; set; }

     public int? AnswerId { get; set; }
        public Answer? Answer { get; set; }

[Required]
   public ReportReason Reason { get; set; }

        [StringLength(1000)]
        public string? Details { get; set; }

        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedAt { get; set; }

        public int? ResolvedByUserId { get; set; }
        public User? ResolvedBy { get; set; }

        [StringLength(500)]
        public string? Resolution { get; set; }
    }

    public enum ReportReason
    {
        [Display(Name = "Spam ho?c qu?ng cáo")]
        Spam = 1,

        [Display(Name = "N?i dung không phù h?p")]
        Inappropriate = 2,

        [Display(Name = "Ngôn t? thù ??ch ho?c l?ng m?")]
   HateSpeech = 3,

  [Display(Name = "Thông tin sai l?ch")]
        Misinformation = 4,

        [Display(Name = "Vi ph?m b?n quy?n")]
        Copyright = 5,

        [Display(Name = "N?i dung trùng l?p")]
        Duplicate = 6,

        [Display(Name = "Lý do khác")]
        Other = 99
    }

    public enum ReportStatus
    {
        [Display(Name = "?ang ch? x? lý")]
        Pending = 0,

        [Display(Name = "?ang xem xét")]
        UnderReview = 1,

        [Display(Name = "?ã gi?i quy?t - H?p l?")]
        ResolvedValid = 2,

        [Display(Name = "?ã gi?i quy?t - Không h?p l?")]
        ResolvedInvalid = 3,

        [Display(Name = "?ã b? qua")]
        Dismissed = 4
    }
}
