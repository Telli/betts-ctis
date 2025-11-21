using BettsTax.Shared;
using BettsTax.Core.DTOs.Compliance;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BettsTax.Core.Services.Interfaces
{
    /// <summary>
    /// Compliance Monitoring Workflow Service - Manages compliance tracking and deadline monitoring
    /// </summary>
    public interface IComplianceMonitoringWorkflow
    {
        /// <summary>
        /// Monitor all deadlines and send alerts
        /// </summary>
        Task<Result> MonitorDeadlinesAsync();

        /// <summary>
        /// Update compliance status for a filing
        /// </summary>
        Task<Result> UpdateComplianceStatusAsync(Guid complianceMonitoringId, string status);

        /// <summary>
        /// Generate compliance alert
        /// </summary>
        Task<Result<ComplianceMonitoringAlertDto>> GenerateComplianceAlertAsync(
            Guid complianceMonitoringId,
            string alertType);

        /// <summary>
        /// Calculate penalty for overdue filing
        /// </summary>
        Task<Result<decimal>> CalculatePenaltyAsync(
            Guid complianceMonitoringId,
            int daysOverdue);

        /// <summary>
        /// Get compliance monitoring for a client
        /// </summary>
        Task<Result<List<ComplianceMonitoringDto>>> GetClientComplianceAsync(int clientId);

        /// <summary>
        /// Get compliance monitoring for a specific tax year
        /// </summary>
        Task<Result<List<ComplianceMonitoringDto>>> GetTaxYearComplianceAsync(int taxYearId);

        /// <summary>
        /// Get pending compliance items
        /// </summary>
        Task<Result<List<ComplianceMonitoringDto>>> GetPendingComplianceAsync();

        /// <summary>
        /// Get overdue compliance items
        /// </summary>
        Task<Result<List<ComplianceMonitoringDto>>> GetOverdueComplianceAsync();

        /// <summary>
        /// Create compliance monitoring entry
        /// </summary>
        Task<Result<ComplianceMonitoringDto>> CreateComplianceMonitoringAsync(
            int clientId,
            int taxYearId,
            string taxType,
            DateTime dueDate,
            decimal amount);

        /// <summary>
        /// Mark filing as filed
        /// </summary>
        Task<Result> MarkAsFiledAsync(Guid complianceMonitoringId, DateTime filedDate);

        /// <summary>
        /// Mark filing as paid
        /// </summary>
        Task<Result> MarkAsPaidAsync(Guid complianceMonitoringId, DateTime paidDate);

        /// <summary>
        /// Get compliance statistics
        /// </summary>
        Task<Result<ComplianceStatisticsDto>> GetComplianceStatisticsAsync(
            int? clientId = null,
            DateTime? from = null,
            DateTime? to = null);

        /// <summary>
        /// Get compliance alerts for a monitoring item
        /// </summary>
        Task<Result<List<ComplianceMonitoringAlertDto>>> GetAlertsAsync(Guid complianceMonitoringId);

        /// <summary>
        /// Get penalty calculations for a monitoring item
        /// </summary>
        Task<Result<List<CompliancePenaltyCalculationDto>>> GetPenaltyCalculationsAsync(Guid complianceMonitoringId);
    }
}

