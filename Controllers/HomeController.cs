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
        private readonly IQueryService _queryService;
        private readonly IUserService _userService;
        private readonly AppDbContext _context;
        private readonly IGamificationService _gamificationService;

        public HomeController(
            ILogger<HomeController> logger,
            IQueryService queryService,
            IUserService userService,
            AppDbContext context,
            IGamificationService gamificationService)
        {
            _logger = logger;
            _queryService = queryService;
            _userService = userService;
            _context = context;
            _gamificationService = gamificationService;
        }

        public async Task<IActionResult> Index(string? search, string? tag, string? sort)
        {
            sort ??= "trending";
            var model = await _queryService.GetQuestionsAsync(search, tag, sort);
            return View(model);
        }

        public async Task<IActionResult> Leaderboard()
        {
            var model = await _userService.GetLeaderboardAsync(100);
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
        public IActionResult Error(int? statusCode = null)
        {
            var viewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            if (statusCode.HasValue)
            {
                if (statusCode == 404)
                {
                    ViewBag.ErrorTitle = "404 - Not Found";
                    ViewBag.ErrorMessage = "Trang bạn tìm kiếm không tồn tại hoặc đã bị xóa.";
                }
                else
                {
                    ViewBag.ErrorTitle = $"Error {statusCode}";
                    ViewBag.ErrorMessage = "Đã xảy ra lỗi khi xử lý yêu cầu của bạn.";
                }
            }
            else
            {
                ViewBag.ErrorTitle = "500 - Server Error";
                ViewBag.ErrorMessage = "Hệ thống gặp sự cố bất ngờ. Đội ngũ kỹ thuật đang khắc phục.";
            }

            return View(viewModel);
        }

        private static List<BadgeStatusViewModel> BuildBadges(UserProfileViewModel profile)
        {
            var userBadges = profile.Badges.ToHashSet();
            var allBadges = Enum.GetValues<BadgeType>().ToList();
            var result = new List<BadgeStatusViewModel>();

            foreach (var type in allBadges)
            {
                var meta = Helpers.BadgeHelper.GetMetadata(type);
                var isEarned = userBadges.Contains(type);
                int progress = 0;
                int target = 1;
                
                switch (type)
                {
                    case BadgeType.Curious: target = 1; progress = profile.QuestionCount; break;
                    case BadgeType.Inquisitive: target = 25; progress = profile.QuestionCount; break;
                    case BadgeType.Questioner: target = 100; progress = profile.QuestionCount; break;
                    
                    case BadgeType.Teacher: target = 1; progress = profile.AnswerCount; break;
                    case BadgeType.Educator: target = 30; progress = profile.AnswerCount; break;
                    case BadgeType.Scholar: target = 150; progress = profile.AnswerCount; break;

                    case BadgeType.Supporter: target = 20; progress = profile.VoteCount; break;
                    case BadgeType.Critic: target = 200; progress = profile.VoteCount; break;
                    case BadgeType.Judge: target = 1000; progress = profile.VoteCount; break;

                    case BadgeType.NiceAnswer: target = 10; progress = 0; break; 
                    case BadgeType.GoodAnswer: target = 50; progress = 0; break;
                    case BadgeType.GreatAnswer: target = 100; progress = 0; break;

                    case BadgeType.Accepted: target = 1; progress = profile.AcceptedAnswerCount > 0 ? 1 : 0; break;

                    case BadgeType.Enthusiast: target = 7; progress = 0; break; 
                    case BadgeType.Dedicated: target = 60; progress = 0; break;
                    case BadgeType.Fanatic: target = 365; progress = 0; break;

                    case BadgeType.Popular: target = 500; progress = 0; break;
                    case BadgeType.Famous: target = 5000; progress = 0; break;
                }

                if (isEarned) progress = target;
                if (progress > target) progress = target;

                result.Add(new BadgeStatusViewModel
                {
                    Tier = meta.Tier == Helpers.BadgeTier.Gold ? "🥇 Vàng" : 
                           meta.Tier == Helpers.BadgeTier.Silver ? "🥈 Bạc" : "🥉 Đồng",
                    TierRaw = meta.Tier,
                    Name = meta.Name,
                    Description = meta.Description,
                    Target = target,
                    Progress = progress,
                    RewardXp = meta.Tier == Helpers.BadgeTier.Gold ? 150 :
                               meta.Tier == Helpers.BadgeTier.Silver ? 80 : 40,
                    Achieved = isEarned,
                    Icon = meta.Icon
                });
            }

            return result.OrderByDescending(b => b.Achieved).ThenByDescending(b => b.RewardXp).ToList();
        }

        private int GetCurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var userId) ? userId : 0;
        }
    }
}