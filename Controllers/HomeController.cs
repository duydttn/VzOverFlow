using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Models;
using VzOverFlow.Models.ViewModels;
using VzOverFlow.Services;

namespace VzOverFlow.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IQuestionService _questionService;
        private readonly IUserService _userService;
        private readonly AppDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            IQuestionService questionService,
            IUserService userService,
            AppDbContext context)
        {
            _logger = logger;
            _questionService = questionService;
            _userService = userService;
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? tag, string? sort)
        {
            var model = await _questionService.GetQuestionsAsync(search, tag, sort);
            return View(model);
        }

        public async Task<IActionResult> Leaderboard()
        {
            var model = await _userService.GetLeaderboardAsync(20);
            return View(model);
        }

        public async Task<IActionResult> Badges()
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return View(new BadgePageViewModel());
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return View(new BadgePageViewModel());
            }

            var profile = await _userService.GetUserProfileAsync(userId);
            if (profile == null)
            {
                return View(new BadgePageViewModel());
            }

            var badges = BuildBadges(profile);
            return View(new BadgePageViewModel
            {
                UserName = profile.UserName,
                Badges = badges
            });
        }

        public async Task<IActionResult> Learn()
        {
            var paths = await _context.Tags
                .AsNoTracking()
                .OrderByDescending(t => t.Questions.Count)
                .Select(t => new LearningPathViewModel
                {
                    TagName = t.Name,
                    QuestionCount = t.Questions.Count,
                    AnswerCount = t.Questions.Sum(q => q.Answers.Count)
                })
                .Take(6)
                .ToListAsync();

            return View(paths);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static List<BadgeStatusViewModel> BuildBadges(UserProfileViewModel profile)
        {
            var definitions = new[]
            {
                new BadgeStatusViewModel
                {
                    Tier = "🥇 Vàng",
                    Name = "Legendary",
                    Description = "Đạt 1000 reputation.",
                    Target = 1000,
                    Progress = profile.Reputation,
                    RewardXp = 150
                },
                new BadgeStatusViewModel
                {
                    Tier = "🥈 Bạc",
                    Name = "Helpful",
                    Description = "Trả lời 25 câu hỏi.",
                    Target = 25,
                    Progress = profile.AnswerCount,
                    RewardXp = 80
                },
                new BadgeStatusViewModel
                {
                    Tier = "🥈 Bạc",
                    Name = "Accepted Mentor",
                    Description = "Có 5 câu trả lời được chấp nhận.",
                    Target = 5,
                    Progress = profile.AcceptedAnswerCount,
                    RewardXp = 60
                },
                new BadgeStatusViewModel
                {
                    Tier = "🥉 Đồng",
                    Name = "Supporter",
                    Description = "Vote ít nhất 10 lần cho câu hỏi/câu trả lời.",
                    Target = 10,
                    Progress = profile.VoteCount,
                    RewardXp = 40
                }
            };

            return definitions.ToList();
        }

        private int GetCurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var userId) ? userId : 0;
        }
    }
}
