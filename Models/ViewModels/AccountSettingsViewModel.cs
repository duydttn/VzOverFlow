using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

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

        [StringLength(500)]
        [Display(Name = "Giới thiệu bản thân")]
        public string? Bio { get; set; }

        [StringLength(500)]
        [Display(Name = "Ảnh đại diện (URL)")]
        public string? AvatarUrl { get; set; }

        [Display(Name = "Tải ảnh lên")]
        public IFormFile? AvatarFile { get; set; }

        public string? StatusMessage { get; set; }
    }
}