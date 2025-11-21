using System;
using System.Collections.Generic;

namespace BettsTax.Core.DTOs.Compliance
{
    /// <summary>
    /// Compliance Monitoring DTO
    /// </summary>
    public class ComplianceMonitoringDto
    {
        public Guid Id { get; set; }
        public int ClientId { get; set; }
        public int TaxYearId { get; set; }
        public string TaxType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime? FiledDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public decimal Amount { get; set; }
        public decimal? EstimatedPenalty { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Compliance Monitoring Alert DTO
    /// </summary>
    public class ComplianceMonitoringAlertDto
    {
        public Guid Id { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public string? SentTo { get; set; }
    }

    /// <summary>
    /// Compliance Penalty Calculation DTO
    /// </summary>
    public class CompliancePenaltyCalculationDto
    {
        public Guid Id { get; set; }
        public string PenaltyType { get; set; } = string.Empty;
        public decimal BaseAmount { get; set; }
        public decimal PenaltyRate { get; set; }
        public decimal CalculatedPenalty { get; set; }
        public int DaysOverdue { get; set; }
        public string CalculationBasis { get; set; } = string.Empty;
    }

    /// <summary>
    /// Compliance Statistics DTO
    /// </summary>
    public class ComplianceStatisticsDto
    {
        public int TotalItems { get; set; }
        public int FiledCount { get; set; }
        public int PaidCount { get; set; }
        public int OverdueCount { get; set; }
        public int PendingCount { get; set; }
        public decimal ComplianceRate { get; set; }
        public decimal TotalPenalties { get; set; }
        public int AverageDaysOverdue { get; set; }
    }

    /// <summary>
    /// Request to create compliance monitoring
    /// </summary>
    public class CreateComplianceMonitoringRequest
    {
        public int ClientId { get; set; }
        public int TaxYearId { get; set; }
        public string TaxType { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request to mark filing as filed
    /// </summary>
    public class MarkAsFiledRequest
    {
        public Guid ComplianceMonitoringId { get; set; }
        public DateTime FiledDate { get; set; }
    }

    /// <summary>
    /// Request to mark filing as paid
    /// </summary>
    public class MarkAsPaidRequest
    {
        public Guid ComplianceMonitoringId { get; set; }
        public DateTime PaidDate { get; set; }
    }

    /// <summary>
    /// Request to update compliance status
    /// </summary>
    public class UpdateComplianceStatusRequest
    {
        public Guid ComplianceMonitoringId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

