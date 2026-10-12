using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Models;
using VzOverFlow.Models.ViewModels;

namespace VzOverFlow.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public UserService(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IEnumerable<User>> GetUsersAsync(string? search = null)
        {
            var adminRoleId = await _context.Roles
                .Where(r => r.Name == "Admin")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var query = _context.Users.AsNoTracking();

            query = query.Where(u => !_context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == adminRoleId));

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    u.UserName.Contains(search) ||
                    (u.Email ?? string.Empty).Contains(search));
            }

            return await query
                .Include(u => u.Questions)
                .Include(u => u.Answers)
                .Include(u => u.Badges)
                .OrderByDescending(u => u.Reputation)
                .ThenByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<UserProfileViewModel?> GetUserProfileAsync(int userId, int currentUserId = 0)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Questions)
                    .ThenInclude(q => q.Votes)
                .Include(u => u.Questions)
                    .ThenInclude(q => q.Tags)
                .Include(u => u.Answers)
                    .ThenInclude(a => a.Votes)
                .Include(u => u.Answers)
                    .ThenInclude(a => a.Question)
                .Include(u => u.Votes)
                .Include(u => u.Badges)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return null;
            }

            var questionVotesReceived = user.Questions.Sum(q => q.Votes.Sum(v => v.Value));
            var answerVotesReceived = user.Answers.Sum(a => a.Votes.Sum(v => v.Value));

            bool isFollowing = false;
            if (currentUserId > 0 && currentUserId != userId)
            {
                isFollowing = await _context.UserFollows.AnyAsync(f => f.FollowerId == currentUserId && f.FollowedUserId == userId);
            }

            var questionsList = user.Questions
                .OrderByDescending(q => q.CreatedAt)
                .Select(q => new QuestionListItemViewModel
                {
                    Id = q.Id,
                    Title = q.Title,
                    VoteScore = q.Votes.Sum(v => v.Value),
                    AnswerCount = q.Answers?.Count ?? 0,
                    ViewCount = q.ViewCount,
                    CreatedAt = q.CreatedAt,
                    UserName = user.UserName,
                    UserReputation = user.Reputation,
                    UserId = user.Id,
                    AvatarUrl = user.AvatarUrl,
                    Tags = q.Tags.Select(t => t.Name).ToList()
                })
                .ToList();

            var answersList = user.Answers
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AnswerListItemViewModel
                {
                    Id = a.Id,
                    QuestionId = a.QuestionId,
                    QuestionTitle = a.Question.Title,
                    BodyPreview = a.Body.Length > 200 ? a.Body.Substring(0, 200) + "..." : a.Body,
                    VoteScore = a.Votes.Sum(v => v.Value),
                    CreatedAt = a.CreatedAt,
                    IsAccepted = a.IsAccepted
                })
                .ToList();

            var today = DateTime.UtcNow.Date;
            var todayMission = await _context.DailyMissions
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId && d.Date == today);

            return new UserProfileViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email ?? string.Empty,
                CreatedAt = user.CreatedAt,
                QuestionCount = user.Questions.Count,
                AnswerCount = user.Answers.Count,
                Reputation = user.Reputation,
                ExperiencePoints = user.ExperiencePoints,
                VoteCount = user.Votes.Count,
                AcceptedAnswerCount = user.Answers.Count(a => a.IsAccepted),
                QuestionVotesReceived = questionVotesReceived,
                AnswerVotesReceived = answerVotesReceived,
                TwoFactorEnabled = user.TwoFactorEnabled,
                IsFollowing = isFollowing,
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl,
                TodayMission = todayMission,
                Badges = user.Badges.Select(b => b.Badge).ToList(),
                Questions = questionsList,
                Answers = answersList
            };
        }

        public async Task<IEnumerable<UserProfileViewModel>> GetLeaderboardAsync(int take = 100)
        {
            var adminRoleId = await _context.Roles
                .Where(r => r.Name == "Admin")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            return await _context.Users
                .AsNoTracking()
                .Where(u => !_context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == adminRoleId))
                .OrderByDescending(u => u.Reputation)
                .ThenBy(u => u.UserName)
                .Take(take)
                .Select(u => new UserProfileViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email ?? string.Empty,
                    AvatarUrl = u.AvatarUrl,
                    CreatedAt = u.CreatedAt,
                    QuestionCount = u.Questions.Count,
                    AnswerCount = u.Answers.Count,
                    Reputation = u.Reputation,
                    TwoFactorEnabled = u.TwoFactorEnabled,
                    Badges = u.Badges.Select(b => b.Badge).ToList()
                })
                .ToListAsync();
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleUserBanAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsBanned = !user.IsBanned;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AdjustReputationAsync(int userId, int amount)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.Reputation = Math.Max(0, user.Reputation + amount);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeUserRoleAsync(int userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, roleName);
            
            return true;
        }

        public async Task<AdminDashboardStatsViewModel> GetAdminDashboardStatsAsync()
        {
            return new AdminDashboardStatsViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalQuestions = await _context.Questions.CountAsync(),
                TotalAnswers = await _context.Answers.CountAsync(),
                TotalReports = await _context.Reports.CountAsync(),
                PendingReports = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Pending),
                BannedUsers = await _context.Users.CountAsync(u => u.IsBanned),
                RecentUsers = await _context.Users.OrderByDescending(u => u.CreatedAt).Take(5).ToListAsync()
            };
        }
    }
}