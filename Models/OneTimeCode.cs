using System;
using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models
{
    public enum OtpPurpose
    {
        Login = 0,
        EnableTwoFactor = 1,
        DisableTwoFactor = 2,
        ChangePassword = 3,
        EmailVerification = 4 // Xác th?c email khi ??ng ký
    }

    public class OneTimeCode
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = default!;

        [Required, StringLength(6)]
        public string Code { get; set; } = default!;

        [Required]
        public OtpPurpose Purpose { get; set; }

        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
    }
}

