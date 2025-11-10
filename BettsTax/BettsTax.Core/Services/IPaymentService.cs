using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    public interface IPaymentService
    {
        Task<PagedResult<PaymentDto>> GetPaymentsAsync(int page, int pageSize, string? searchTerm = null, PaymentStatus? status = null, int? clientId = null);
        Task<PaymentDto?> GetPaymentByIdAsync(int id);
        Task<IEnumerable<PaymentDto>> GetClientPaymentsAsync(int clientId);
        Task<List<PaymentDto>> GetPendingApprovalsAsync();
        Task<PaymentDto> CreateAsync(CreatePaymentDto dto, string userId);
        Task<PaymentDto> UpdateAsync(int id, CreatePaymentDto dto, string userId);
        Task<bool> DeleteAsync(int id, string userId);
        Task<PaymentDto> ApproveAsync(int paymentId, ApprovePaymentDto dto, string approverId);
        Task<PaymentDto> RejectAsync(int paymentId, RejectPaymentDto dto, string approverId);
    Task<DocumentDto> UploadEvidenceAsync(int paymentId, UploadPaymentEvidenceDto dto, Microsoft.AspNetCore.Http.IFormFile file, string userId);
    Task<PaymentDto> ReconcileAsync(int paymentId, ReconcilePaymentDto dto, string userId);
        Task<decimal> GetTotalPaidByClientAsync(int clientId, int? taxYear = null);
        Task<List<PaymentDto>> GetPaymentsByTaxFilingAsync(int taxFilingId);
    }
}
