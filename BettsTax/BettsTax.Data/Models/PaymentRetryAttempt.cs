namespace BettsTax.Data.Models;

public class PaymentRetryAttempt
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public int AttemptNumber { get; set; }
    public DateTime AttemptedAt { get; set; }
    public string PaymentGateway { get; set; } = string.Empty;
    public string AttemptStatus { get; set; } = string.Empty; // Success, Failed, Timeout
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? GatewayResponse { get; set; }
    public string? TransactionId { get; set; }
    public decimal AttemptAmount { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public string ClientIpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Additional properties for retry logic
    public string Status { get; set; } = string.Empty;
    public string AttemptedBy { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public PaymentTransaction? Transaction { get; set; }
}