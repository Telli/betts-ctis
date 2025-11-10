using System.ComponentModel.DataAnnotations;

namespace BettsTax.Core.DTOs
{
    public class SystemSettingDto
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = "General";
        public bool IsEncrypted { get; set; } = false;
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedByUserId { get; set; } = string.Empty;
        public string UpdatedByUserName { get; set; } = string.Empty;
    }

    public class EmailSettingsDto
    {
        [Required]
        [StringLength(200)]
        public string SmtpHost { get; set; } = string.Empty;

        [Required]
        [Range(1, 65535)]
        public int SmtpPort { get; set; } = 587;

        [Required]
        [StringLength(200)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string FromEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FromName { get; set; } = string.Empty;

        public bool UseSSL { get; set; } = true;
        public bool UseTLS { get; set; } = true;
    }

    public class TestEmailDto
    {
        [Required]
        [EmailAddress]
        public string ToEmail { get; set; } = string.Empty;
        
        public string Subject { get; set; } = "Test Email from The Betts Firm";
        public string Body { get; set; } = "This is a test email to verify your email configuration.";
    }

    public class TaxSettingsDto
    {
        // Annual turnover threshold (SLE) at which GST registration is required
        [Range(0, double.MaxValue)]
        public decimal GstRegistrationThreshold { get; set; } = 0m;

        // GST standard rate percent (e.g., 15 = 15%)
        [Range(0, 100)]
        public decimal GstRatePercent { get; set; } = 15m;

        // Annual interest rate for late payments (percent)
        [Range(0, 100)]
        public decimal AnnualInterestRatePercent { get; set; } = 15m;

        // Minimum tax for companies (percent of turnover)
        [Range(0, 100)]
        public decimal IncomeMinimumTaxRatePercent { get; set; } = 0.5m;

        // Minimum Alternate Tax (percent of turnover)
        [Range(0, 100)]
        public decimal IncomeMatRatePercent { get; set; } = 3m;
    }
}