using System.ComponentModel.DataAnnotations;
using BettsTax.Data;

namespace BettsTax.Core.DTOs
{
    public class ClientInvitationDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ClientRegistrationDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string BusinessName { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? TaxpayerIdentificationNumber { get; set; }

        [Required]
        public TaxpayerCategory TaxpayerCategory { get; set; }

        [Required]
        public ClientType ClientType { get; set; }

        [Required]
        public string RegistrationToken { get; set; } = string.Empty;

        public string? BusinessAddress { get; set; }
        public string? ContactPersonName { get; set; }
        public string? ContactPersonPhone { get; set; }
        public decimal? AnnualTurnover { get; set; }
    }

    public class SelfRegistrationDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class EmailVerificationDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }

    public class TokenValidationResult
    {
        public bool IsValid { get; set; }
        public string? Email { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }

    public class ResendVerificationDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}