namespace VzOverFlow.Models.ViewModels
{
    public class UserMiniCardViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = default!;
        public string? AvatarUrl { get; set; }
        public int UserReputation { get; set; }
        public string? Bio { get; set; }
    }
}