namespace VzOverFlow.Models.ViewModels
{
    public class BadgesViewModel
    {
  public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
  public int TotalXP { get; set; }
 public List<BadgeProgressItem> Badges { get; set; } = new();
    }

    public class BadgeProgressItem
    {
        public BadgeType BadgeType { get; set; }
        public string Name { get; set; } = string.Empty;
   public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
     public string Category { get; set; } = string.Empty;
        public bool IsEarned { get; set; }
  public DateTime? EarnedAt { get; set; }
 public int CurrentProgress { get; set; }
   public int RequiredProgress { get; set; }
 public int XpReward { get; set; }
}
}
