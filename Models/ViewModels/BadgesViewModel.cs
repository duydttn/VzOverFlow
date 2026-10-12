using VzOverFlow.Helpers;

namespace VzOverFlow.Models.ViewModels
{
    public class BadgePageViewModel
    {
        public bool RequiresLogin { get; set; } = false;
        public string UserName { get; set; } = string.Empty;
        public List<BadgeStatusViewModel> Badges { get; set; } = new();
    }

    public class BadgeStatusViewModel
    {
        public string Tier { get; set; } = string.Empty;
        public BadgeTier TierRaw { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Target { get; set; }
        public int Progress { get; set; }
        public int RewardXp { get; set; }
        public bool Achieved { get; set; }
    }
}
