using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data
{
    /// <summary>
    /// Compliance monitoring workflow entity - tracks compliance status and deadlines
    /// </summary>
    public class ComplianceMonitoringWorkflow
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        public int TaxYearId { get; set; }

        [Required]
        [MaxLength(100)]
        public string TaxType { get; set; } = string.Empty; // GST, PAYE, Income Tax, etc.

        [Required]
        public ComplianceMonitoringStatus Status { get; set; } = ComplianceMonitoringStatus.Pending;

        [Required]
        public DateTime DueDate { get; set; }

        public DateTime? FiledDate { get; set; }

        public DateTime? PaidDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? EstimatedPenalty { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public bool IsOverdue { get; set; } = false;

        public int DaysOverdue { get; set; } = 0;

        public bool AlertSent30Days { get; set; } = false;
        public bool AlertSent14Days { get; set; } = false;
        public bool AlertSent10Days { get; set; } = false;
        public bool AlertSent7Days { get; set; } = false;
        public bool AlertSent1Day { get; set; } = false;
        public bool AlertSentOverdue { get; set; } = false;
        public DateTime? LastDailyReminderSent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(450)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public Client? Client { get; set; }
        public TaxYear? TaxYear { get; set; }
        public List<ComplianceMonitoringAlert> Alerts { get; set; } = new();
    }

    /// <summary>
    /// Compliance monitoring alert entity - tracks alerts sent for compliance items
    /// </summary>
    public class ComplianceMonitoringAlert
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ComplianceMonitoringWorkflowId { get; set; }

        [Required]
        [MaxLength(100)]
        public string AlertType { get; set; } = string.Empty; // "30DayWarning", "14DayWarning", "7DayWarning", "1DayWarning", "Overdue"

        [Required]
        public ComplianceAlertStatus Status { get; set; } = ComplianceAlertStatus.Sent;

        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? SentTo { get; set; }

        [MaxLength(1000)]
        public string? Response { get; set; }

        public DateTime? ResponseAt { get; set; }

        // Navigation properties
        public ComplianceMonitoringWorkflow? ComplianceMonitoring { get; set; }
    }

    /// <summary>
    /// Compliance penalty calculation entity - tracks penalty calculations
    /// </summary>
    public class CompliancePenaltyCalculation
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ComplianceMonitoringWorkflowId { get; set; }

        [Required]
        [MaxLength(100)]
        public string PenaltyType { get; set; } = string.Empty; // "LateFiling", "LatePayment", "Underpayment"

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PenaltyRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CalculatedPenalty { get; set; }

        [MaxLength(500)]
        public string CalculationBasis { get; set; } = string.Empty; // Description of how penalty was calculated

        public int DaysOverdue { get; set; }

        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? CalculatedBy { get; set; }

        // Navigation properties
        public ComplianceMonitoringWorkflow? ComplianceMonitoring { get; set; }
    }

    /// <summary>
    /// Compliance monitoring status enum
    /// </summary>
    public enum ComplianceMonitoringStatus
    {
        Pending = 0,
        Filed = 1,
        Paid = 2,
        Overdue = 3,
        Compliant = 4,
        NonCompliant = 5,
        Exempted = 6
    }

    /// <summary>
    /// Compliance alert status enum
    /// </summary>
    public enum ComplianceAlertStatus
    {
        Pending = 0,
        Sent = 1,
        Acknowledged = 2,
        Ignored = 3,
        Failed = 4
    }
}

