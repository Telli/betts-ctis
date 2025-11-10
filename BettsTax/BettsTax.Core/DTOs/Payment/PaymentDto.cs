namespace BettsTax.Core.DTOs.Payment;

/// <summary>
/// Payment DTO
/// </summary>
public class PaymentDto
{
    public int Id { get; set; }
    public string Client { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string ReceiptNo { get; set; } = string.Empty;
}

/// <summary>
/// Payment summary DTO
/// </summary>
public class PaymentSummaryDto
{
    public decimal TotalAmount { get; set; }
    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
    public int PendingCount { get; set; }
}

/// <summary>
/// Create payment DTO
/// </summary>
public class CreatePaymentDto
{
    public string Client { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
}
