using AutoMapper;
using BettsTax.Core.DTOs;
using BettsTax.Core.DTOs.Reports;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PaymentService> _logger;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly ISmsService _smsService;
        private readonly IActivityTimelineService _activityService;
        private readonly IReportGenerator _reportGenerator;
        private readonly IDocumentService _documentService;

        public PaymentService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<PaymentService> logger,
            IAuditService auditService,
            INotificationService notificationService,
            ISmsService smsService,
            IActivityTimelineService activityService,
            IReportGenerator reportGenerator,
            IDocumentService documentService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
            _notificationService = notificationService;
            _smsService = smsService;
            _activityService = activityService;
            _reportGenerator = reportGenerator;
            _documentService = documentService;
        }

        public async Task<PagedResult<PaymentDto>> GetPaymentsAsync(
            int page, 
            int pageSize, 
            string? searchTerm = null, 
            PaymentStatus? status = null, 
            int? clientId = null)
        {
            var query = _context.Payments
                .Include(p => p.Client)
                .Include(p => p.TaxFiling)
                // Removed Include for ApprovedBy because the navigation is ignored in the model
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => 
                    p.PaymentReference.Contains(searchTerm) ||
                    p.Client!.BusinessName.Contains(searchTerm) ||
                    p.Client!.ClientNumber.Contains(searchTerm));
            }

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            if (clientId.HasValue)
                query = query.Where(p => p.ClientId == clientId.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<PaymentDto>>(items);

            return new PagedResult<PaymentDto>
            {
                Items = dtos,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PaymentDto?> GetPaymentByIdAsync(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Client)
                .Include(p => p.TaxFiling)
                // Removed Include for ApprovedBy because the navigation is ignored in the model
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            return payment == null ? null : _mapper.Map<PaymentDto>(payment);
        }

        public async Task<IEnumerable<PaymentDto>> GetClientPaymentsAsync(int clientId)
        {
            var payments = await _context.Payments
                .Include(p => p.Client)
                .Include(p => p.TaxFiling)
                // Removed Include for ApprovedBy because the navigation is ignored in the model
                .Where(p => p.ClientId == clientId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
        }

        public async Task<List<PaymentDto>> GetPendingApprovalsAsync()
        {
            var payments = await _context.Payments
                .Include(p => p.Client)
                .Include(p => p.TaxFiling)
                .Where(p => p.Status == PaymentStatus.Pending)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<PaymentDto>>(payments);
        }

        public async Task<PaymentDto> CreateAsync(CreatePaymentDto dto, string userId)
        {
            // Validate client exists
            var client = await _context.Clients.FindAsync(dto.ClientId);
            if (client == null)
                throw new InvalidOperationException("Client not found");

            // Generate payment reference if not provided
            var paymentReference = string.IsNullOrEmpty(dto.PaymentReference) ?
                GeneratePaymentReference(dto.ClientId) : dto.PaymentReference;

            var payment = new Payment
            {
                ClientId = dto.ClientId,
                TaxYearId = dto.TaxYearId,
                TaxFilingId = dto.TaxFilingId,
                Amount = dto.Amount,
                Method = dto.Method,
                PaymentReference = paymentReference,
                PaymentDate = dto.PaymentDate,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ApprovalWorkflow = "PendingReview"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "CREATE", "Payment", payment.PaymentId.ToString(),
                $"Created payment {paymentReference} of {payment.Amount:C} for client {client.BusinessName}");

            // Notification (only if client has a linked user account)
            if (!string.IsNullOrEmpty(client.UserId))
            {
                await _notificationService.CreateAsync(client.UserId,
                    $"Payment of {payment.Amount:C} created and pending approval.");
            }

            _logger.LogInformation("Created payment {PaymentReference} for client {ClientId}", 
                paymentReference, dto.ClientId);

            return await GetPaymentByIdAsync(payment.PaymentId) ??
                throw new InvalidOperationException("Failed to retrieve created payment");
        }

        public async Task<PaymentDto> UpdateAsync(int id, CreatePaymentDto dto, string userId)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                throw new InvalidOperationException("Payment not found");

            // Only allow updates if pending
            if (payment.Status != PaymentStatus.Pending)
                throw new InvalidOperationException("Can only update pending payments");

            // Update fields
            payment.Amount = dto.Amount;
            payment.Method = dto.Method;
            payment.PaymentReference = dto.PaymentReference;
            payment.PaymentDate = dto.PaymentDate;
            payment.TaxYearId = dto.TaxYearId;
            payment.TaxFilingId = dto.TaxFilingId;

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "UPDATE", "Payment", payment.PaymentId.ToString(),
                $"Updated payment {payment.PaymentReference}");

            _logger.LogInformation("Updated payment {PaymentId}", id);

            return await GetPaymentByIdAsync(id) ??
                throw new InvalidOperationException("Failed to retrieve updated payment");
        }

        public async Task<bool> DeleteAsync(int id, string userId)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return false;

            // Only allow deletion if pending
            if (payment.Status != PaymentStatus.Pending)
                throw new InvalidOperationException("Can only delete pending payments");

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "DELETE", "Payment", payment.PaymentId.ToString(),
                $"Deleted payment {payment.PaymentReference}");

            _logger.LogInformation("Deleted payment {PaymentId}", id);

            return true;
        }

        public async Task<PaymentDto> ApproveAsync(int paymentId, ApprovePaymentDto dto, string approverId)
        {
            var payment = await _context.Payments.Include(p => p.Client).FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (payment == null)
                throw new InvalidOperationException("Payment not found");

            if (payment.Status != PaymentStatus.Pending)
                throw new InvalidOperationException("Only pending payments can be approved");

            payment.Status = PaymentStatus.Approved;
            payment.ApprovedAt = DateTime.UtcNow;
            payment.ApprovedById = approverId;
            payment.ApprovalWorkflow = $"Approved on {DateTime.UtcNow:yyyy-MM-dd}. {dto.Comments}";

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(approverId, "APPROVE", "Payment", payment.PaymentId.ToString(),
                $"Approved payment {payment.PaymentReference} of {payment.Amount:C}");

            // Notification
            if (payment.Client != null)
            {
                if (!string.IsNullOrEmpty(payment.Client.UserId))
                {
                    await _notificationService.CreateAsync(payment.Client.UserId,
                        $"Payment {payment.PaymentReference} of {payment.Amount:C} has been approved.");
                }
                
                // Send SMS confirmation
                await _smsService.SendPaymentConfirmationAsync(paymentId);
                
                // Log activity
                await _activityService.LogPaymentActivityAsync(paymentId, ActivityType.PaymentProcessed);
            }

            _logger.LogInformation("Approved payment {PaymentId} by user {ApproverId}", paymentId, approverId);

            // Attempt to generate and store a payment receipt document (non-blocking on failure)
            try
            {
                await GenerateAndStoreReceiptAsync(payment, approverId);
            }
            catch (Exception rex)
            {
                _logger.LogError(rex, "Failed to generate/store receipt for payment {PaymentId}", paymentId);
                // Do not rethrow – approval succeeded, receipt is a best-effort add-on
            }

            return await GetPaymentByIdAsync(paymentId) ??
                throw new InvalidOperationException("Failed to retrieve approved payment");
        }

        private async Task GenerateAndStoreReceiptAsync(Payment payment, string userId)
        {
            // Build a minimal receipt report payload
            var data = new ReportDataDto
            {
                Title = "Payment Receipt",
                Subtitle = payment.Client != null ? $"Client: {payment.Client.BusinessName} (ID {payment.ClientId})" : $"Client ID: {payment.ClientId}",
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = userId,
                Data = new Dictionary<string, object>
                {
                    ["ReceiptReference"] = payment.PaymentReference,
                    ["PaymentId"] = payment.PaymentId,
                    ["ClientId"] = payment.ClientId,
                    ["TaxFilingId"] = payment.TaxFilingId ?? 0,
                    ["TaxYearId"] = payment.TaxYearId ?? 0,
                    ["TaxType"] = payment.TaxType?.ToString() ?? "N/A",
                    ["AmountSLE"] = payment.Amount,
                    ["Method"] = payment.Method.ToString(),
                    ["Status"] = payment.Status.ToString(),
                    ["PaymentDate"] = payment.PaymentDate.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };

            // Generate PDF-like content (SimpleReportGenerator returns text bytes as a PDF alternative)
            var bytes = await _reportGenerator.GeneratePdfReportAsync(data, templateName: "paymentreceipt");

            // Create an in-memory IFormFile to reuse existing upload path
            var fileName = $"Receipt_{payment.PaymentReference}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            using var ms = new MemoryStream(bytes);
            IFormFile file = new FormFile(ms, 0, bytes.Length, name: "file", fileName: fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var upload = new UploadDocumentDto
            {
                ClientId = payment.ClientId,
                TaxYearId = payment.TaxYearId,
                TaxFilingId = payment.TaxFilingId,
                Category = DocumentCategory.Receipt,
                Description = $"Payment receipt for reference {payment.PaymentReference}"
            };

            // Store document; this logs audit + activity internally
            await _documentService.UploadAsync(upload, file, userId);

            // Also log a payment activity note about receipt creation
            await _activityService.LogPaymentActivityAsync(payment.PaymentId, ActivityType.PaymentProcessed, "Receipt generated and stored");
        }

        public async Task<PaymentDto> RejectAsync(int paymentId, RejectPaymentDto dto, string approverId)
        {
            var payment = await _context.Payments.Include(p => p.Client).FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (payment == null)
                throw new InvalidOperationException("Payment not found");

            if (payment.Status != PaymentStatus.Pending)
                throw new InvalidOperationException("Only pending payments can be rejected");

            payment.Status = PaymentStatus.Rejected;
            payment.ApprovedAt = DateTime.UtcNow;
            payment.ApprovedById = approverId;
            payment.RejectionReason = dto.RejectionReason;
            payment.ApprovalWorkflow = $"Rejected on {DateTime.UtcNow:yyyy-MM-dd}. Reason: {dto.RejectionReason}";

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(approverId, "REJECT", "Payment", payment.PaymentId.ToString(),
                $"Rejected payment {payment.PaymentReference} - Reason: {dto.RejectionReason}");

            // Notification
            if (payment.Client != null)
            {
                if (!string.IsNullOrEmpty(payment.Client.UserId))
                {
                    await _notificationService.CreateAsync(payment.Client.UserId,
                        $"Payment {payment.PaymentReference} has been rejected. Reason: {dto.RejectionReason}");
                }
            }

            _logger.LogInformation("Rejected payment {PaymentId} by user {ApproverId}", paymentId, approverId);

            return await GetPaymentByIdAsync(paymentId) ??
                throw new InvalidOperationException("Failed to retrieve rejected payment");
        }

        public async Task<DocumentDto> UploadEvidenceAsync(int paymentId, UploadPaymentEvidenceDto dto, IFormFile file, string userId)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (payment == null)
                throw new InvalidOperationException("Payment not found");

            if (payment.ClientId != dto.ClientId)
                throw new InvalidOperationException("Client mismatch for payment evidence upload");

            // Reuse document service to store the evidence
            var upload = new UploadDocumentDto
            {
                ClientId = dto.ClientId,
                TaxYearId = dto.TaxYearId,
                TaxFilingId = dto.TaxFilingId,
                Category = dto.Category,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? $"Payment evidence for {payment.PaymentReference}" : dto.Description
            };

            var document = await _documentService.UploadAsync(upload, file, userId);

            // Log activity against the payment
            await _activityService.LogPaymentActivityAsync(payment.PaymentId, ActivityType.DocumentUploaded, "Payment evidence uploaded");

            return document;
        }

        public async Task<PaymentDto> ReconcileAsync(int paymentId, ReconcilePaymentDto dto, string userId)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (payment == null)
                throw new InvalidOperationException("Payment not found");

            // Only allow reconciliation for Approved or Pending bank/cash/cheque payments
            if (payment.Status == PaymentStatus.Rejected)
                throw new InvalidOperationException("Cannot reconcile a rejected payment");

            payment.IsReconciled = true;
            payment.ReconciledAt = DateTime.UtcNow;
            payment.ReconciledBy = userId;
            payment.ReconciliationReference = dto.ReconciliationReference;
            payment.BankStatementReference = dto.BankStatementReference;
            payment.BankStatementDate = dto.BankStatementDate;
            payment.ReconciliationNotes = dto.Notes;

            if (dto.MarkAsCompleted)
            {
                payment.Status = PaymentStatus.Completed;
                payment.CompletedAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Audit + activity
            await _auditService.LogAsync(userId, "RECONCILE", "Payment", payment.PaymentId.ToString(),
                $"Reconciled payment {payment.PaymentReference} (Amount {payment.Amount:C})");
            await _activityService.LogPaymentActivityAsync(payment.PaymentId, ActivityType.PaymentProcessed, "Payment reconciled");

            return await GetPaymentByIdAsync(paymentId) ??
                throw new InvalidOperationException("Failed to retrieve reconciled payment");
        }

        public async Task<decimal> GetTotalPaidByClientAsync(int clientId, int? taxYear = null)
        {
            var query = _context.Payments
                .Where(p => p.ClientId == clientId && p.Status == PaymentStatus.Approved);

            if (taxYear.HasValue)
            {
                query = query.Where(p => p.PaymentDate.Year == taxYear.Value);
            }

            return await query.SumAsync(p => p.Amount);
        }

        public async Task<List<PaymentDto>> GetPaymentsByTaxFilingAsync(int taxFilingId)
        {
            var payments = await _context.Payments
                .Include(p => p.Client)
                // Removed Include for ApprovedBy because the navigation is ignored in the model
                .Where(p => p.TaxFilingId == taxFilingId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return _mapper.Map<List<PaymentDto>>(payments);
        }

        private string GeneratePaymentReference(int clientId)
        {
            return $"PAY-{clientId:D6}-{DateTime.UtcNow:yyyyMMddHHmm}";
        }
    }
}
