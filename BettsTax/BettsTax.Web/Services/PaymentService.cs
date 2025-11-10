using BettsTax.Core.DTOs.Payment;
using BettsTax.Core.Services.Interfaces;

namespace BettsTax.Web.Services;

/// <summary>
/// Payment service implementation
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
    }

    public async Task<List<PaymentDto>> GetPaymentsAsync(int? clientId = null)
    {
        _logger.LogInformation("Retrieving payments for clientId={ClientId}", clientId);

        await Task.CompletedTask;

        // Mock data - replace with actual database query
        var payments = new List<PaymentDto>
        {
            new() { Id = 1, Client = "Sierra Leone Breweries Ltd", TaxType = "VAT", Period = "Q1 2025", Amount = 45000.00m, Method = "Bank Transfer", Status = "Completed", Date = "2025-01-15", ReceiptNo = "RCP-2025-001" },
            new() { Id = 2, Client = "Standard Chartered Bank SL", TaxType = "Corporate Tax", Period = "Q4 2024", Amount = 125000.00m, Method = "Direct Debit", Status = "Completed", Date = "2025-01-10", ReceiptNo = "RCP-2025-002" },
            new() { Id = 3, Client = "Orange Sierra Leone", TaxType = "Withholding Tax", Period = "December 2024", Amount = 32000.00m, Method = "Bank Transfer", Status = "Pending", Date = "2025-01-20", ReceiptNo = "RCP-2025-003" },
            new() { Id = 4, Client = "Rokel Commercial Bank", TaxType = "VAT", Period = "Q1 2025", Amount = 28000.00m, Method = "Check", Status = "Completed", Date = "2025-01-12", ReceiptNo = "RCP-2025-004" },
            new() { Id = 5, Client = "Freetown Terminal Ltd", TaxType = "Corporate Tax", Period = "Q4 2024", Amount = 18500.00m, Method = "Bank Transfer", Status = "Processing", Date = "2025-01-18", ReceiptNo = "RCP-2025-005" }
        };

        // Filter by clientId if provided
        if (clientId.HasValue)
        {
            // In real implementation, would filter by actual client ID
            // For mock, just return first payment for demo
            return payments.Take(1).ToList();
        }

        return payments;
    }

    public async Task<PaymentSummaryDto> GetPaymentSummaryAsync(int? clientId = null)
    {
        _logger.LogInformation("Retrieving payment summary for clientId={ClientId}", clientId);

        var payments = await GetPaymentsAsync(clientId);

        var summary = new PaymentSummaryDto
        {
            TotalAmount = payments.Sum(p => p.Amount),
            TotalCount = payments.Count,
            CompletedCount = payments.Count(p => p.Status == "Completed"),
            PendingCount = payments.Count(p => p.Status == "Pending" || p.Status == "Processing")
        };

        return summary;
    }

    public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto dto, int? clientId = null)
    {
        _logger.LogInformation("Creating new payment for client={Client}, amount={Amount}", dto.Client, dto.Amount);

        await Task.CompletedTask;

        // Mock implementation - replace with actual database insert
        var newPayment = new PaymentDto
        {
            Id = new Random().Next(100, 999),
            Client = dto.Client,
            TaxType = dto.TaxType,
            Period = dto.Period,
            Amount = dto.Amount,
            Method = dto.Method,
            Status = "Pending",
            Date = DateTime.Now.ToString("yyyy-MM-dd"),
            ReceiptNo = $"RCP-{DateTime.Now.Year}-{new Random().Next(100, 999):D3}"
        };

        return newPayment;
    }
}
