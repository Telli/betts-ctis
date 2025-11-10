using BettsTax.Core.DTOs.Payment;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace BettsTax.Web.Services;

/// <summary>
/// Payment service implementation
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ApplicationDbContext context, ILogger<PaymentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<PaymentDto>> GetPaymentsAsync(int? clientId = null)
    {
        _logger.LogInformation("Retrieving payments for clientId={ClientId}", clientId);

        var query = _context.Payments
            .Include(p => p.Client)
            .AsQueryable();

        if (clientId.HasValue)
        {
            query = query.Where(p => p.ClientId == clientId.Value);
        }

        var payments = await query
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                Client = p.Client.Name,
                TaxType = p.TaxType,
                Period = p.Period,
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status,
                Date = p.Date.ToString("yyyy-MM-dd"),
                ReceiptNo = p.ReceiptNo
            })
            .OrderByDescending(p => p.Date)
            .ToListAsync();

        return payments;
    }

    public async Task<PaymentSummaryDto> GetPaymentSummaryAsync(int? clientId = null)
    {
        _logger.LogInformation("Retrieving payment summary for clientId={ClientId}", clientId);

        var query = _context.Payments.AsQueryable();

        if (clientId.HasValue)
        {
            query = query.Where(p => p.ClientId == clientId.Value);
        }

        var totalAmount = await query.SumAsync(p => (decimal?)p.Amount) ?? 0;
        var totalCount = await query.CountAsync();
        var completedCount = await query.CountAsync(p => p.Status == "Completed");
        var pendingCount = await query.CountAsync(p => p.Status == "Pending" || p.Status == "Processing");

        return new PaymentSummaryDto
        {
            TotalAmount = totalAmount,
            TotalCount = totalCount,
            CompletedCount = completedCount,
            PendingCount = pendingCount
        };
    }

    public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto dto, int? clientId = null)
    {
        _logger.LogInformation("Creating new payment for client={Client}, amount={Amount}", dto.Client, dto.Amount);

        int effectiveClientId;
        if (clientId.HasValue)
        {
            effectiveClientId = clientId.Value;
        }
        else
        {
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Name == dto.Client);
            if (client == null)
            {
                throw new InvalidOperationException($"Client '{dto.Client}' not found");
            }
            effectiveClientId = client.Id;
        }

        var payment = new Models.Entities.Payment
        {
            ClientId = effectiveClientId,
            TaxType = dto.TaxType,
            Period = dto.Period,
            Amount = dto.Amount,
            Method = dto.Method,
            Status = "Pending",
            Date = DateTime.UtcNow,
            ReceiptNo = $"RCP-{DateTime.UtcNow.Year}-{new Random().Next(100, 999):D3}",
            IsDemo = false
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var clientName = await _context.Clients
            .Where(c => c.Id == effectiveClientId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync() ?? dto.Client;

        return new PaymentDto
        {
            Id = payment.Id,
            Client = clientName,
            TaxType = payment.TaxType,
            Period = payment.Period,
            Amount = payment.Amount,
            Method = payment.Method,
            Status = payment.Status,
            Date = payment.Date.ToString("yyyy-MM-dd"),
            ReceiptNo = payment.ReceiptNo
        };
    }
}
