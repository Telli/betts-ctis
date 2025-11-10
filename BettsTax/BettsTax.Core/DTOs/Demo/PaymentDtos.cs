namespace BettsTax.Core.DTOs.Demo;

/// <summary>
/// Represents a recorded tax payment.
/// </summary>
public class PaymentRecordDto
{
    public int Id { get; set; }
    public int? ClientId { get; set; }
    public string Client { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
}

/// <summary>
/// Aggregated totals for payment statuses.
/// </summary>
public class PaymentSummaryDto
{
    public decimal Paid { get; set; }
    public decimal Pending { get; set; }
    public decimal Overdue { get; set; }
}

/// <summary>
/// Response shape for payment listings with summary information.
/// </summary>
public class PaymentsResponseDto
{
    public IReadOnlyList<PaymentRecordDto> Items { get; init; } = Array.Empty<PaymentRecordDto>();
    public PaymentSummaryDto Summary { get; init; } = new();
}
