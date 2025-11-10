using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data;

public class TaxAuthoritySubmission
{
    public int Id { get; set; }
    public int TaxFilingId { get; set; }
    public string AuthorityReference { get; set; } = string.Empty;
    public string SubmissionStatus { get; set; } = "Pending"; // Pending, Submitted, Accepted, Rejected
    public string? SubmissionResponse { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AuthorityStatus { get; set; }
    public DateTime? StatusLastChecked { get; set; }

    // Navigation properties
    public TaxFiling TaxFiling { get; set; } = null!;
}

public class TaxAuthorityStatusCheck
{
    public int Id { get; set; }
    public int TaxFilingId { get; set; }
    public string AuthorityReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CheckedAt { get; set; }
    public bool IsSuccessful { get; set; }

    // Navigation properties
    public TaxFiling TaxFiling { get; set; } = null!;
}

// API Request/Response models
public class TaxAuthoritySubmissionRequest
{
    [Required]
    public string TaxpayerTin { get; set; } = string.Empty;

    [Required]
    public string TaxType { get; set; } = string.Empty;

    [Required]
    public string TaxPeriod { get; set; } = string.Empty;

    [Required]
    public decimal TaxAmount { get; set; }

    public decimal? PenaltyAmount { get; set; }

    public decimal? InterestAmount { get; set; }

    public DateTime DueDate { get; set; }

    public string? AdditionalData { get; set; }
}

public class TaxAuthoritySubmissionResponse
{
    public bool Success { get; set; }
    public string? Reference { get; set; }
    public string? Message { get; set; }
    public string? Status { get; set; }
    public DateTime? Timestamp { get; set; }
}

public class TaxAuthorityStatusRequest
{
    [Required]
    public string Reference { get; set; } = string.Empty;
}

public class TaxAuthorityStatusResponse
{
    public bool Success { get; set; }
    public string? Status { get; set; }
    public string? Details { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string? Message { get; set; }
}