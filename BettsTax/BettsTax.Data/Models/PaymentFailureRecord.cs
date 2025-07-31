namespace BettsTax.Data.Models;

public class PaymentFailureRecord
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    
    // Additional property for transaction reference
    public int TransactionId { get; set; }
    
    public string FailureType { get; set; } = string.Empty; // Gateway, Network, Validation, Business
    public string FailureCode { get; set; } = string.Empty;
    public string FailureMessage { get; set; } = string.Empty;
    
    // Reason property (alias for FailureMessage)
    public string Reason { get; set; } = string.Empty;
    
    public string PaymentGateway { get; set; } = string.Empty;
    public string? GatewayErrorCode { get; set; }
    public string? GatewayErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public string? RequestData { get; set; }
    public string? ResponseData { get; set; }
    public int TotalRetryAttempts { get; set; }
    public bool IsRecoverable { get; set; }
    public bool RequiresManualIntervention { get; set; }
    public DateTime FailedAt { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
    
    // Additional property for who handled the failure
    public string? HandledBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}