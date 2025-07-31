using BettsTax.Data.Models;

namespace BettsTax.Core.DTOs.Payment;

// Webhook log DTOs
public class PaymentWebhookLogDto
{
    public int Id { get; set; }
    public int GatewayConfigId { get; set; }
    public string GatewayName { get; set; } = string.Empty;
    public WebhookEventType EventType { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public string RequestBody { get; set; } = string.Empty;
    public string RequestHeaders { get; set; } = string.Empty;
    public string? ResponseBody { get; set; }
    public int ResponseStatusCode { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessingError { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? SignatureHeader { get; set; }
    public bool? IsSignatureValid { get; set; }
}

// Webhook search DTOs
public class PaymentWebhookSearchDto
{
    public int? GatewayConfigId { get; set; }
    public WebhookEventType? EventType { get; set; }
    public string? TransactionReference { get; set; }
    public bool? IsProcessed { get; set; }
    public bool? IsSignatureValid { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? IpAddress { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "ReceivedAt";
    public string SortDirection { get; set; } = "desc";
}