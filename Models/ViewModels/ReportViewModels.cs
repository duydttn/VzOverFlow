using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models.ViewModels
{
    public class CreateReportViewModel
    {
   public int? QuestionId { get; set; }
        public int? AnswerId { get; set; }

        [Required(ErrorMessage = "Vui lòng ch?n lý do báo cáo")]
        public ReportReason Reason { get; set; }

 [StringLength(1000, ErrorMessage = "Chi ti?t không ???c quá 1000 ký t?")]
 public string? Details { get; set; }

      // For display
public string? ContentPreview { get; set; }
        public string? ContentType { get; set; }
    }

    public class ReportListViewModel
    {
        public List<ReportItemViewModel> Reports { get; set; } = new();
        public ReportStatus? FilterStatus { get; set; }
        public int TotalCount { get; set; }
 public int PendingCount { get; set; }
    }

    public class ReportItemViewModel
    {
        public int Id { get; set; }
        public string ReporterName { get; set; } = string.Empty;
        public ReportReason Reason { get; set; }
  public string? Details { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
   
        // Content info
 public int? QuestionId { get; set; }
        public string? QuestionTitle { get; set; }
        public int? AnswerId { get; set; }
        public string? ContentPreview { get; set; }
    }
}
