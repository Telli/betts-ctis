using BettsTax.Data;
using BettsTax.Shared;

namespace BettsTax.Core.Services.Payments;

/// <summary>
/// Abstraction for external/local payment gateway providers.
/// </summary>
public interface IPaymentGateway
{
    PaymentProvider Provider { get; }
    Task<Result<PaymentGatewayResponse>> InitiateAsync(PaymentGatewayRequest request, CancellationToken ct = default);
    Task<Result<PaymentGatewayResponse>> GetStatusAsync(string providerTransactionId, CancellationToken ct = default);
    Task<Result<PaymentGatewayResponse>> RefundAsync(string providerTransactionId, decimal amount, CancellationToken ct = default);
    Task<Result<bool>> ValidateWebhookAsync(string rawBody, string signatureHeader, CancellationToken ct = default);
    Task<Result<PaymentGatewayResponse>> ProcessWebhookAsync(string rawBody, CancellationToken ct = default);
}

public class PaymentGatewayRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SLE";
    public string TransactionReference { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Description { get; set; }
    public string CallbackUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

public class PaymentGatewayResponse
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? ProviderReference { get; set; }
    public PaymentTransactionStatus Status { get; set; }
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PaymentUrl { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Fee { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}