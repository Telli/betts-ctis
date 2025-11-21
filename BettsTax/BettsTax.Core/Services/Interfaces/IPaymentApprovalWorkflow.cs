using BettsTax.Shared;
using BettsTax.Core.DTOs.Payments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BettsTax.Core.Services.Interfaces
{
    /// <summary>
    /// Payment Approval Workflow Service - Manages multi-level payment approvals
    /// </summary>
    public interface IPaymentApprovalWorkflow
    {
        /// <summary>
        /// Request payment approval based on amount thresholds
        /// </summary>
        Task<Result<PaymentApprovalRequestDto>> RequestPaymentApprovalAsync(
            int paymentId,
            decimal amount,
            string requestedBy);

        /// <summary>
        /// Get pending approvals for a specific approver
        /// </summary>
        Task<Result<List<PaymentApprovalRequestDto>>> GetPendingApprovalsAsync(
            string approverId);

        /// <summary>
        /// Get all pending approvals across all approvers
        /// </summary>
        Task<Result<List<PaymentApprovalRequestDto>>> GetAllPendingApprovalsAsync();

        /// <summary>
        /// Get approval request details
        /// </summary>
        Task<Result<PaymentApprovalRequestDto>> GetApprovalRequestAsync(
            Guid approvalRequestId);

        /// <summary>
        /// Approve a payment at current approval level
        /// </summary>
        Task<Result<PaymentApprovalRequestDto>> ApprovePaymentAsync(
            Guid approvalRequestId,
            string approverId,
            string? comments = null);

        /// <summary>
        /// Reject a payment approval
        /// </summary>
        Task<Result<PaymentApprovalRequestDto>> RejectPaymentAsync(
            Guid approvalRequestId,
            string approverId,
            string rejectionReason);

        /// <summary>
        /// Delegate approval to another user
        /// </summary>
        Task<Result<PaymentApprovalRequestDto>> DelegateApprovalAsync(
            Guid approvalRequestId,
            string currentApproverId,
            string delegateToUserId,
            string? reason = null);

        /// <summary>
        /// Get approval thresholds
        /// </summary>
        Task<Result<List<PaymentApprovalThresholdDto>>> GetApprovalThresholdsAsync();

        /// <summary>
        /// Create or update approval threshold
        /// </summary>
        Task<Result<PaymentApprovalThresholdDto>> UpsertApprovalThresholdAsync(
            PaymentApprovalThresholdDto threshold,
            string userId);

        /// <summary>
        /// Get approval chain for a specific amount
        /// </summary>
        Task<Result<List<string>>> GetApprovalChainAsync(decimal amount);

        /// <summary>
        /// Get approval history for a payment
        /// </summary>
        Task<Result<List<PaymentApprovalStepDto>>> GetApprovalHistoryAsync(
            Guid approvalRequestId);

        /// <summary>
        /// Cancel an approval request
        /// </summary>
        Task<Result> CancelApprovalAsync(
            Guid approvalRequestId,
            string cancelledBy,
            string reason);

        /// <summary>
        /// Get approval statistics
        /// </summary>
        Task<Result<PaymentApprovalStatisticsDto>> GetApprovalStatisticsAsync(
            DateTime? from = null,
            DateTime? to = null);
    }
}

