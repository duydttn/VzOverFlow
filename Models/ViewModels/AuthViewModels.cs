using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required, StringLength(100)]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc để nhận mã OTP")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(200)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Tên đăng nhập hoặc email")]
        public string UserNameOrEmail { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;
    }
}