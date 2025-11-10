using System.ComponentModel.DataAnnotations;

namespace BettsTax.Web.Options;

public class TaxAuthorityOptions
{
    public const string SectionName = "TaxAuthority";

    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public int TimeoutSeconds { get; set; } = 30;

    public bool EnableStatusChecks { get; set; } = true;

    public bool EnableAutoSubmission { get; set; } = false;

    public int StatusCheckIntervalMinutes { get; set; } = 60;

    public Dictionary<string, string> Endpoints { get; set; } = new();

    public Dictionary<string, string> Headers { get; set; } = new();
}