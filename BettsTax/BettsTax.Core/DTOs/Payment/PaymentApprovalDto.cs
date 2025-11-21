using System;
using System.Collections.Generic;

namespace BettsTax.Core.DTOs.Payments
{
    /// <summary>
    /// Payment Approval Request DTO
    /// </summary>
    public class PaymentApprovalRequestDto
    {
        public Guid Id { get; set; }
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Comments { get; set; }
        public List<string> ApprovalChain { get; set; } = new();
        public int CurrentApprovalLevel { get; set; }
        public string? CurrentApproverId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? RejectionReason { get; set; }
        public List<PaymentApprovalStepDto> ApprovalSteps { get; set; } = new();
    }

    /// <summary>
    /// Payment Approval Step DTO
    /// </summary>
    public class PaymentApprovalStepDto
    {
        public Guid Id { get; set; }
        public int ApprovalLevel { get; set; }
        public string ApprovalRole { get; set; } = string.Empty;
        public string ApproverId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Comments { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? RejectionReason { get; set; }
    }

    /// <summary>
    /// Payment Approval Threshold DTO
    /// </summary>
    public class PaymentApprovalThresholdDto
    {
        public Guid Id { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public List<string> ApprovalChain { get; set; } = new();
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Payment Approval Statistics DTO
    /// </summary>
    public class PaymentApprovalStatisticsDto
    {
        public int TotalRequests { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int PendingCount { get; set; }
        public decimal AverageApprovalTime { get; set; }
        public decimal TotalAmountApproved { get; set; }
        public decimal TotalAmountRejected { get; set; }
        public decimal ApprovalRate { get; set; }
    }

    /// <summary>
    /// Request to approve a payment
    /// </summary>
    public class ApprovePaymentApprovalRequest
    {
        public Guid ApprovalRequestId { get; set; }
        public string? Comments { get; set; }
    }

    /// <summary>
    /// Request to reject a payment
    /// </summary>
    public class RejectPaymentApprovalRequest
    {
        public Guid ApprovalRequestId { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to delegate approval
    /// </summary>
    public class DelegatePaymentApprovalRequest
    {
        public Guid ApprovalRequestId { get; set; }
        public string DelegateToUserId { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Request to create/update approval threshold
    /// </summary>
    public class CreatePaymentApprovalThresholdRequest
    {
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public List<string> ApprovalChain { get; set; } = new();
        public string Description { get; set; } = string.Empty;
    }
}

