using System.Collections.Generic;

namespace VzOverFlow.Models.ViewModels
{
    public class AdminDashboardStatsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalAnswers { get; set; }
        public int TotalReports { get; set; }
        public int PendingReports { get; set; }
        public int BannedUsers { get; set; }
        public List<User> RecentUsers { get; set; } = new List<User>();
    }

    public class AdminUsersViewModel
    {
        public AdminDashboardStatsViewModel Stats { get; set; } = new AdminDashboardStatsViewModel();
        public IEnumerable<User> Users { get; set; } = new List<User>();
        public string? SearchQuery { get; set; }
    }
}