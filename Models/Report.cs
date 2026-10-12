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
        [Display(Name = "Spam hoặc quảng cáo")]
        Spam = 1,

        [Display(Name = "Nội dung không phù hợp")]
        Inappropriate = 2,

        [Display(Name = "Ngôn từ thù địch hoặc lăng mạ")]
        HateSpeech = 3,

        [Display(Name = "Thông tin sai lệch")]
        Misinformation = 4,

        [Display(Name = "Vi phạm bản quyền")]
        Copyright = 5,

        [Display(Name = "Nội dung trùng lặp")]
        Duplicate = 6,

        [Display(Name = "Lý do khác")]
        Other = 99
    }

    public enum ReportStatus
    {
        [Display(Name = "Đang chờ xử lý")]
        Pending = 0,

        [Display(Name = "Đang xem xét")]
        UnderReview = 1,

        [Display(Name = "Đã giải quyết - Hợp lệ")]
        ResolvedValid = 2,

        [Display(Name = "Đã giải quyết - Không hợp lệ")]
        ResolvedInvalid = 3,

        [Display(Name = "Đã bỏ qua")]
        Dismissed = 4
    }
}