using BettsTax.Data;
using BettsTax.Shared;
using BettsTax.Core.DTOs.Payments;
using BettsTax.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BettsTax.Core.Services
{
    /// <summary>
    /// Payment Approval Workflow Service - Manages multi-level payment approvals
    /// </summary>
    public class PaymentApprovalWorkflow : IPaymentApprovalWorkflow
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;
        private readonly ILogger<PaymentApprovalWorkflow> _logger;

        public PaymentApprovalWorkflow(
            ApplicationDbContext context,
            INotificationService notificationService,
            IAuditService auditService,
            ILogger<PaymentApprovalWorkflow> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<Result<PaymentApprovalRequestDto>> RequestPaymentApprovalAsync(
            int paymentId, decimal amount, string requestedBy)
        {
            try
            {
                _logger.LogInformation("Requesting payment approval for payment {PaymentId}, amount {Amount}", paymentId, amount);

                // Get payment
                var payment = await _context.Payments.FindAsync(paymentId);
                if (payment == null)
                    return Result.Failure<PaymentApprovalRequestDto>("Payment not found");

                // Determine approval chain based on amount
                var approvalChain = await DetermineApprovalChainAsync(amount);
                if (approvalChain.Count == 0)
                    return Result.Failure<PaymentApprovalRequestDto>("No approval chain configured for this amount");

                // Create approval request
                var approvalRequest = new PaymentApprovalRequest
                {
                    Id = Guid.NewGuid(),
                    PaymentId = paymentId,
                    Amount = amount,
                    RequestedBy = requestedBy,
                    Status = PaymentApprovalStatus.Pending,
                    ApprovalChain = JsonSerializer.Serialize(approvalChain),
                    CurrentApprovalLevel = 0,
                    CreatedAt = DateTime.UtcNow,
                    SubmittedAt = DateTime.UtcNow
                };

                // Create approval steps
                for (int i = 0; i < approvalChain.Count; i++)
                {
                    var step = new PaymentApprovalStep
                    {
                        Id = Guid.NewGuid(),
                        ApprovalRequestId = approvalRequest.Id,
                        ApprovalLevel = i + 1,
                        ApprovalRole = approvalChain[i],
                        ApproverId = await GetApproverForRoleAsync(approvalChain[i]),
                        Status = i == 0 ? PaymentApprovalStepStatus.Pending : PaymentApprovalStepStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };
                    approvalRequest.ApprovalSteps.Add(step);
                }

                // Set current approver
                var firstStep = approvalRequest.ApprovalSteps.FirstOrDefault();
                if (firstStep != null)
                {
                    approvalRequest.CurrentApproverId = firstStep.ApproverId;
                    approvalRequest.CurrentApprovalLevel = 1;
                }

                _context.PaymentApprovalRequests.Add(approvalRequest);
                await _context.SaveChangesAsync();

                // Notify first approver
                if (firstStep != null)
                {
                    await NotifyApproverAsync(approvalRequest, firstStep);
                }

                // Audit log
                await _auditService.LogAsync(requestedBy, "CREATE", "PaymentApprovalRequest", approvalRequest.Id.ToString(),
                    $"Created payment approval request for payment {paymentId}, amount {amount:C}");

                _logger.LogInformation("Payment approval request created: {ApprovalRequestId}", approvalRequest.Id);

                return Result.Success(MapToDto(approvalRequest));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting payment approval");
                return Result.Failure<PaymentApprovalRequestDto>($"Error requesting payment approval: {ex.Message}");
            }
        }

        public async Task<Result<List<PaymentApprovalRequestDto>>> GetPendingApprovalsAsync(string approverId)
        {
            try
            {
                var approvals = await _context.PaymentApprovalRequests
                    .Where(a => a.Status == PaymentApprovalStatus.Pending && a.CurrentApproverId == approverId)
                    .Include(a => a.ApprovalSteps)
                    .Include(a => a.Payment)
                    .ToListAsync();

                return Result.Success(approvals.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending approvals");
                return Result.Failure<List<PaymentApprovalRequestDto>>($"Error getting pending approvals: {ex.Message}");
            }
        }

        public async Task<Result<List<PaymentApprovalRequestDto>>> GetAllPendingApprovalsAsync()
        {
            try
            {
                var approvals = await _context.PaymentApprovalRequests
                    .Where(a => a.Status == PaymentApprovalStatus.Pending)
                    .Include(a => a.ApprovalSteps)
                    .Include(a => a.Payment)
                    .ToListAsync();

                return Result.Success(approvals.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all pending approvals");
                return Result.Failure<List<PaymentApprovalRequestDto>>($"Error getting all pending approvals: {ex.Message}");
            }
        }

        public async Task<Result<PaymentApprovalRequestDto>> GetApprovalRequestAsync(Guid approvalRequestId)
        {
            try
            {
                var approval = await _context.PaymentApprovalRequests
                    .Include(a => a.ApprovalSteps)
                    .Include(a => a.Payment)
                    .FirstOrDefaultAsync(a => a.Id == approvalRequestId);

                if (approval == null)
                    return Result.Failure<PaymentApprovalRequestDto>("Approval request not found");

                return Result.Success(MapToDto(approval));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approval request");
                return Result.Failure<PaymentApprovalRequestDto>($"Error getting approval request: {ex.Message}");
            }
        }

        public async Task<Result<PaymentApprovalRequestDto>> ApprovePaymentAsync(
            Guid approvalRequestId, string approverId, string? comments = null)
        {
            try
            {
                _logger.LogInformation("Approving payment: {ApprovalRequestId} by {ApproverId}", approvalRequestId, approverId);

                var approval = await _context.PaymentApprovalRequests
                    .Include(a => a.ApprovalSteps)
                    .FirstOrDefaultAsync(a => a.Id == approvalRequestId);

                if (approval == null)
                    return Result.Failure<PaymentApprovalRequestDto>("Approval request not found");

                if (approval.CurrentApproverId != approverId)
                    return Result.Failure<PaymentApprovalRequestDto>("You are not the current approver");

                // Mark current step as approved
                var currentStep = approval.ApprovalSteps.FirstOrDefault(s => s.ApprovalLevel == approval.CurrentApprovalLevel);
                if (currentStep != null)
                {
                    currentStep.Status = PaymentApprovalStepStatus.Approved;
                    currentStep.ReviewedAt = DateTime.UtcNow;
                    currentStep.Comments = comments;
                }

                // Check if all steps are approved
                if (approval.CurrentApprovalLevel >= approval.ApprovalSteps.Count)
                {
                    approval.Status = PaymentApprovalStatus.Approved;
                    approval.CompletedAt = DateTime.UtcNow;
                    approval.CompletedBy = approverId;

                    // Update payment status
                    var payment = await _context.Payments.FindAsync(approval.PaymentId);
                    if (payment != null)
                    {
                        payment.Status = PaymentStatus.Approved;
                        payment.ApprovedAt = DateTime.UtcNow;
                        payment.ApprovedById = approverId;
                    }
                }
                else
                {
                    // Move to next approval level
                    approval.CurrentApprovalLevel++;
                    var nextStep = approval.ApprovalSteps.FirstOrDefault(s => s.ApprovalLevel == approval.CurrentApprovalLevel);
                    if (nextStep != null)
                    {
                        approval.CurrentApproverId = nextStep.ApproverId;
                        await NotifyApproverAsync(approval, nextStep);
                    }
                }

                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(approverId, "APPROVE", "PaymentApprovalRequest", approvalRequestId.ToString(),
                    $"Approved payment approval request");

                _logger.LogInformation("Payment approved: {ApprovalRequestId}", approvalRequestId);

                return Result.Success(MapToDto(approval));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving payment");
                return Result.Failure<PaymentApprovalRequestDto>($"Error approving payment: {ex.Message}");
            }
        }

        public async Task<Result<PaymentApprovalRequestDto>> RejectPaymentAsync(
            Guid approvalRequestId, string approverId, string rejectionReason)
        {
            try
            {
                _logger.LogInformation("Rejecting payment: {ApprovalRequestId} by {ApproverId}", approvalRequestId, approverId);

                var approval = await _context.PaymentApprovalRequests
                    .Include(a => a.ApprovalSteps)
                    .FirstOrDefaultAsync(a => a.Id == approvalRequestId);

                if (approval == null)
                    return Result.Failure<PaymentApprovalRequestDto>("Approval request not found");

                approval.Status = PaymentApprovalStatus.Rejected;
                approval.RejectionReason = rejectionReason;
                approval.CompletedAt = DateTime.UtcNow;
                approval.CompletedBy = approverId;

                // Mark current step as rejected
                var currentStep = approval.ApprovalSteps.FirstOrDefault(s => s.ApprovalLevel == approval.CurrentApprovalLevel);
                if (currentStep != null)
                {
                    currentStep.Status = PaymentApprovalStepStatus.Rejected;
                    currentStep.ReviewedAt = DateTime.UtcNow;
                    currentStep.RejectionReason = rejectionReason;
                }

                // Update payment status
                var payment = await _context.Payments.FindAsync(approval.PaymentId);
                if (payment != null)
                {
                    payment.Status = PaymentStatus.Rejected;
                    payment.RejectionReason = rejectionReason;
                }

                await _context.SaveChangesAsync();

                // Notify requester
                await _notificationService.SendNotificationAsync(
                    approval.RequestedBy,
                    $"Payment Approval Rejected. Reason: {rejectionReason}",
                    "Email");

                // Audit log
                await _auditService.LogAsync(approverId, "REJECT", "PaymentApprovalRequest", approvalRequestId.ToString(),
                    $"Rejected payment approval request. Reason: {rejectionReason}");

                _logger.LogInformation("Payment rejected: {ApprovalRequestId}", approvalRequestId);

                return Result.Success(MapToDto(approval));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting payment");
                return Result.Failure<PaymentApprovalRequestDto>($"Error rejecting payment: {ex.Message}");
            }
        }

        public async Task<Result<PaymentApprovalRequestDto>> DelegateApprovalAsync(
            Guid approvalRequestId, string currentApproverId, string delegateToUserId, string? reason = null)
        {
            try
            {
                var approval = await _context.PaymentApprovalRequests
                    .Include(a => a.ApprovalSteps)
                    .FirstOrDefaultAsync(a => a.Id == approvalRequestId);

                if (approval == null)
                    return Result.Failure<PaymentApprovalRequestDto>("Approval request not found");

                if (approval.CurrentApproverId != currentApproverId)
                    return Result.Failure<PaymentApprovalRequestDto>("You are not the current approver");

                // Update current step
                var currentStep = approval.ApprovalSteps.FirstOrDefault(s => s.ApprovalLevel == approval.CurrentApprovalLevel);
                if (currentStep != null)
                {
                    currentStep.ApproverId = delegateToUserId;
                    currentStep.Status = PaymentApprovalStepStatus.Delegated;
                }

                approval.CurrentApproverId = delegateToUserId;
                await _context.SaveChangesAsync();

                // Notify new approver
                if (currentStep != null)
                {
                    await NotifyApproverAsync(approval, currentStep);
                }

                return Result.Success(MapToDto(approval));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delegating approval");
                return Result.Failure<PaymentApprovalRequestDto>($"Error delegating approval: {ex.Message}");
            }
        }

        public async Task<Result<List<PaymentApprovalThresholdDto>>> GetApprovalThresholdsAsync()
        {
            try
            {
                var thresholds = await _context.PaymentApprovalThresholds
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.MinAmount)
                    .ToListAsync();

                return Result.Success(thresholds.Select(MapThresholdToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approval thresholds");
                return Result.Failure<List<PaymentApprovalThresholdDto>>($"Error getting approval thresholds: {ex.Message}");
            }
        }

        public async Task<Result<PaymentApprovalThresholdDto>> UpsertApprovalThresholdAsync(
            PaymentApprovalThresholdDto threshold, string userId)
        {
            try
            {
                PaymentApprovalThreshold entity;

                if (threshold.Id == Guid.Empty)
                {
                    entity = new PaymentApprovalThreshold
                    {
                        Id = Guid.NewGuid(),
                        MinAmount = threshold.MinAmount,
                        MaxAmount = threshold.MaxAmount,
                        ApprovalChain = JsonSerializer.Serialize(threshold.ApprovalChain),
                        Description = threshold.Description,
                        IsActive = threshold.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedBy = userId
                    };
                    _context.PaymentApprovalThresholds.Add(entity);
                }
                else
                {
                    entity = await _context.PaymentApprovalThresholds.FindAsync(threshold.Id);
                    if (entity == null)
                        return Result.Failure<PaymentApprovalThresholdDto>("Threshold not found");

                    entity.MinAmount = threshold.MinAmount;
                    entity.MaxAmount = threshold.MaxAmount;
                    entity.ApprovalChain = JsonSerializer.Serialize(threshold.ApprovalChain);
                    entity.Description = threshold.Description;
                    entity.IsActive = threshold.IsActive;
                    entity.UpdatedAt = DateTime.UtcNow;
                    entity.UpdatedBy = userId;
                }

                await _context.SaveChangesAsync();
                return Result.Success(MapThresholdToDto(entity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting approval threshold");
                return Result.Failure<PaymentApprovalThresholdDto>($"Error upserting approval threshold: {ex.Message}");
            }
        }

        public async Task<Result<List<string>>> GetApprovalChainAsync(decimal amount)
        {
            try
            {
                var threshold = await _context.PaymentApprovalThresholds
                    .Where(t => t.IsActive && t.MinAmount <= amount && t.MaxAmount >= amount)
                    .FirstOrDefaultAsync();

                if (threshold == null)
                    return Result.Failure<List<string>>("No approval threshold configured for this amount");

                var chain = JsonSerializer.Deserialize<List<string>>(threshold.ApprovalChain) ?? new List<string>();
                return Result.Success(chain);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approval chain");
                return Result.Failure<List<string>>($"Error getting approval chain: {ex.Message}");
            }
        }

        public async Task<Result<List<PaymentApprovalStepDto>>> GetApprovalHistoryAsync(Guid approvalRequestId)
        {
            try
            {
                var steps = await _context.PaymentApprovalSteps
                    .Where(s => s.ApprovalRequestId == approvalRequestId)
                    .OrderBy(s => s.ApprovalLevel)
                    .ToListAsync();

                return Result.Success(steps.Select(MapStepToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approval history");
                return Result.Failure<List<PaymentApprovalStepDto>>($"Error getting approval history: {ex.Message}");
            }
        }

        public async Task<Result> CancelApprovalAsync(Guid approvalRequestId, string cancelledBy, string reason)
        {
            try
            {
                var approval = await _context.PaymentApprovalRequests.FindAsync(approvalRequestId);
                if (approval == null)
                    return Result.Failure("Approval request not found");

                approval.Status = PaymentApprovalStatus.Cancelled;
                approval.RejectionReason = reason;
                approval.CompletedAt = DateTime.UtcNow;
                approval.CompletedBy = cancelledBy;

                await _context.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling approval");
                return Result.Failure($"Error cancelling approval: {ex.Message}");
            }
        }

        public async Task<Result<PaymentApprovalStatisticsDto>> GetApprovalStatisticsAsync(DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var query = _context.PaymentApprovalRequests.AsQueryable();

                if (from.HasValue)
                    query = query.Where(a => a.CreatedAt >= from.Value);

                if (to.HasValue)
                    query = query.Where(a => a.CreatedAt <= to.Value);

                var approvals = await query.ToListAsync();

                var stats = new PaymentApprovalStatisticsDto
                {
                    TotalRequests = approvals.Count,
                    ApprovedCount = approvals.Count(a => a.Status == PaymentApprovalStatus.Approved),
                    RejectedCount = approvals.Count(a => a.Status == PaymentApprovalStatus.Rejected),
                    PendingCount = approvals.Count(a => a.Status == PaymentApprovalStatus.Pending),
                    TotalAmountApproved = approvals.Where(a => a.Status == PaymentApprovalStatus.Approved).Sum(a => a.Amount),
                    TotalAmountRejected = approvals.Where(a => a.Status == PaymentApprovalStatus.Rejected).Sum(a => a.Amount)
                };

                if (stats.TotalRequests > 0)
                    stats.ApprovalRate = (decimal)stats.ApprovedCount / stats.TotalRequests * 100;

                return Result.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approval statistics");
                return Result.Failure<PaymentApprovalStatisticsDto>($"Error getting approval statistics: {ex.Message}");
            }
        }

        // Helper methods

        private async Task<List<string>> DetermineApprovalChainAsync(decimal amount)
        {
            var threshold = await _context.PaymentApprovalThresholds
                .Where(t => t.IsActive && t.MinAmount <= amount && t.MaxAmount >= amount)
                .FirstOrDefaultAsync();

            if (threshold == null)
                return new List<string>();

            return JsonSerializer.Deserialize<List<string>>(threshold.ApprovalChain) ?? new List<string>();
        }

        private async Task<string> GetApproverForRoleAsync(string role)
        {
            // This would be implemented to get the actual approver for a role
            // For now, return a placeholder
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Role == role);

            return user?.Id ?? string.Empty;
        }

        private async Task NotifyApproverAsync(PaymentApprovalRequest approval, PaymentApprovalStep step)
        {
            var approver = await _context.Users.FindAsync(step.ApproverId);
            if (approver != null)
            {
                await _notificationService.SendNotificationAsync(
                    approver.Id,
                    $"Payment Approval Required: {approval.Amount:C} for Payment ID: {approval.PaymentId}",
                    "Email");
            }
        }

        private PaymentApprovalRequestDto MapToDto(PaymentApprovalRequest approval)
        {
            var chain = JsonSerializer.Deserialize<List<string>>(approval.ApprovalChain) ?? new List<string>();
            return new PaymentApprovalRequestDto
            {
                Id = approval.Id,
                PaymentId = approval.PaymentId,
                Amount = approval.Amount,
                RequestedBy = approval.RequestedBy,
                Status = approval.Status.ToString(),
                Comments = approval.Comments,
                ApprovalChain = chain,
                CurrentApprovalLevel = approval.CurrentApprovalLevel,
                CurrentApproverId = approval.CurrentApproverId,
                CreatedAt = approval.CreatedAt,
                SubmittedAt = approval.SubmittedAt,
                CompletedAt = approval.CompletedAt,
                RejectionReason = approval.RejectionReason,
                ApprovalSteps = approval.ApprovalSteps.Select(MapStepToDto).ToList()
            };
        }

        private PaymentApprovalStepDto MapStepToDto(PaymentApprovalStep step)
        {
            return new PaymentApprovalStepDto
            {
                Id = step.Id,
                ApprovalLevel = step.ApprovalLevel,
                ApprovalRole = step.ApprovalRole,
                ApproverId = step.ApproverId,
                Status = step.Status.ToString(),
                Comments = step.Comments,
                CreatedAt = step.CreatedAt,
                ReviewedAt = step.ReviewedAt,
                RejectionReason = step.RejectionReason
            };
        }

        private PaymentApprovalThresholdDto MapThresholdToDto(PaymentApprovalThreshold threshold)
        {
            var chain = JsonSerializer.Deserialize<List<string>>(threshold.ApprovalChain) ?? new List<string>();
            return new PaymentApprovalThresholdDto
            {
                Id = threshold.Id,
                MinAmount = threshold.MinAmount,
                MaxAmount = threshold.MaxAmount,
                ApprovalChain = chain,
                Description = threshold.Description,
                IsActive = threshold.IsActive
            };
        }
    }
}

