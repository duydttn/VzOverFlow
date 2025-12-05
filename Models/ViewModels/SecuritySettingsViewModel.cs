using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models.ViewModels
{
    public class SecuritySettingsViewModel
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool TwoFactorEnabled { get; set; }
        
        // Authenticator setup - ONLY visible during setup (before 2FA is enabled)
        // After 2FA is enabled, these will be null for security reasons
        public string? AuthenticatorKey { get; set; }
        public string? QrCodeDataUrl { get; set; }
        public string? FormattedKey { get; set; }
        
        public string? StatusMessage { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        public string OldPassword { get; set; } = default!;

        [Required, MinLength(6)]
        public string NewPassword { get; set; } = default!;

        [Required, Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; } = default!;

        [Required, StringLength(6, MinimumLength = 6)]
        public string OtpCode { get; set; } = default!;
    }
}

