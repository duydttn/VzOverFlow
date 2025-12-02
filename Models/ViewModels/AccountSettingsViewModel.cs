using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models.ViewModels
{
    public class AccountSettingsViewModel
    {
        public int UserId { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Tên hiển thị")]
        public string UserName { get; set; } = string.Empty;

        [EmailAddress, StringLength(200)]
        [Display(Name = "Email đăng nhập")]
        public string? Email { get; set; }

        public string? StatusMessage { get; set; }
    }
}


