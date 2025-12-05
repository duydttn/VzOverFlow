using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Models;
using VzOverFlow.Models.ViewModels;

namespace VzOverFlow.Services
{
    public interface IReportService
    {
        Task<Report> CreateReportAsync(int reporterId, CreateReportViewModel model);
   Task<ReportListViewModel> GetReportsAsync(ReportStatus? filterStatus = null);
  Task<Report?> GetReportByIdAsync(int id);
        Task UpdateReportStatusAsync(int reportId, int resolvedByUserId, ReportStatus status, string? resolution);
        Task<bool> HasUserReportedAsync(int userId, int? questionId, int? answerId);
    }

    public class ReportService : IReportService
    {
  private readonly AppDbContext _context;

 public ReportService(AppDbContext context)
 {
            _context = context;
        }

   public async Task<Report> CreateReportAsync(int reporterId, CreateReportViewModel model)
        {
       var report = new Report
   {
      ReporterId = reporterId,
   QuestionId = model.QuestionId,
     AnswerId = model.AnswerId,
    Reason = model.Reason,
                Details = model.Details,
       Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
        };

       _context.Reports.Add(report);
      await _context.SaveChangesAsync();
   return report;
  }

  public async Task<ReportListViewModel> GetReportsAsync(ReportStatus? filterStatus = null)
      {
       var query = _context.Reports
     .AsNoTracking()
         .Include(r => r.Reporter)
        .Include(r => r.Question)
       .Include(r => r.Answer)
      .AsQueryable();

    if (filterStatus.HasValue)
        {
       query = query.Where(r => r.Status == filterStatus.Value);
 }

      var reports = await query
        .OrderByDescending(r => r.CreatedAt)
      .Select(r => new ReportItemViewModel
          {
      Id = r.Id,
           ReporterName = r.Reporter.UserName,
             Reason = r.Reason,
     Details = r.Details,
   Status = r.Status,
          CreatedAt = r.CreatedAt,
  QuestionId = r.QuestionId,
       QuestionTitle = r.Question != null ? r.Question.Title : null,
         AnswerId = r.AnswerId,
       ContentPreview = r.Question != null 
             ? r.Question.Body.Substring(0, Math.Min(150, r.Question.Body.Length))
    : r.Answer != null
          ? r.Answer.Body.Substring(0, Math.Min(150, r.Answer.Body.Length))
    : null
      })
      .ToListAsync();

   var totalCount = await _context.Reports.CountAsync();
      var pendingCount = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Pending);

      return new ReportListViewModel
   {
       Reports = reports,
           FilterStatus = filterStatus,
    TotalCount = totalCount,
       PendingCount = pendingCount
   };
        }

      public async Task<Report?> GetReportByIdAsync(int id)
   {
       return await _context.Reports
  .Include(r => r.Reporter)
         .Include(r => r.Question)
         .Include(r => r.Answer)
      .Include(r => r.ResolvedBy)
           .FirstOrDefaultAsync(r => r.Id == id);
        }

   public async Task UpdateReportStatusAsync(int reportId, int resolvedByUserId, ReportStatus status, string? resolution)
{
       var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
         {
       throw new InvalidOperationException("Report not found");
 }

  report.Status = status;
            report.ResolvedAt = DateTime.UtcNow;
  report.ResolvedByUserId = resolvedByUserId;
   report.Resolution = resolution;

await _context.SaveChangesAsync();
  }

        public async Task<bool> HasUserReportedAsync(int userId, int? questionId, int? answerId)
      {
       return await _context.Reports
    .AnyAsync(r => r.ReporterId == userId && 
         r.QuestionId == questionId && 
               r.AnswerId == answerId);
        }
    }
}
