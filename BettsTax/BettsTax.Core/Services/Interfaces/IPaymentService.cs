using BettsTax.Core.DTOs.Payment;

namespace BettsTax.Core.Services.Interfaces;

/// <summary>
/// Payment service interface
/// </summary>
public interface IPaymentService
{
    Task<List<PaymentDto>> GetPaymentsAsync(int? clientId = null);
    Task<PaymentSummaryDto> GetPaymentSummaryAsync(int? clientId = null);
    Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto dto, int? clientId = null);
}
