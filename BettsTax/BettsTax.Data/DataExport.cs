using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data
{
    public enum ExportStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Expired,
        Deleted
    }

    // Export history tracking
    public class DataExportHistory
    {
        public int DataExportHistoryId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ExportId { get; set; } = string.Empty; // GUID for unique identification
        
        [Required]
        [MaxLength(50)]
        public string ExportType { get; set; } = string.Empty; // TaxReturns, Payments, etc.
        
        [Required]
        [MaxLength(20)]
        public string Format { get; set; } = string.Empty; // Excel, CSV, PDF, etc.
        
        [Required]
        [MaxLength(500)]
        public string FileName { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? FilePath { get; set; }
        
        public long FileSizeBytes { get; set; }
        public int RecordCount { get; set; }
        
        public ExportStatus Status { get; set; } = ExportStatus.Pending;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime? DownloadedDate { get; set; }
        public int DownloadCount { get; set; } = 0;
        
        [Required]
        [MaxLength(450)] // ASP.NET Identity User ID length
        public string CreatedBy { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        public bool IsPasswordProtected { get; set; } = false;
        
        [MaxLength(64)] // SHA256 hash length
        public string? PasswordHash { get; set; }
        
        // Filters and parameters used for the export (JSON)
        public string? FiltersJson { get; set; }
        
        // Error details if export failed
        public string? ErrorDetails { get; set; }
        
        // Processing time in milliseconds
        public long? ProcessingTimeMs { get; set; }
        
        // Navigation properties
        public ApplicationUser? CreatedByUser { get; set; }
    }

    // Export templates for reusable exports
    public class ExportTemplate
    {
        public int ExportTemplateId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string ExportType { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string Format { get; set; } = string.Empty;
        
        // Export configuration (JSON)
        [Required]
        public string ConfigurationJson { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        public bool IsSystemTemplate { get; set; } = false; // System vs user-created
        
        [Required]
        [MaxLength(450)]
        public string CreatedBy { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastRunDate { get; set; }
        public int RunCount { get; set; } = 0;
        
        // Scheduling information (future enhancement)
        public bool IsScheduled { get; set; } = false;
        [MaxLength(500)]
        public string? ScheduleCron { get; set; } // Cron expression for scheduling
        public DateTime? NextRunDate { get; set; }
        
        // Navigation properties
        public ApplicationUser? CreatedByUser { get; set; }
        public List<DataExportHistory> ExportHistory { get; set; } = new();
    }

    // Export queue for background processing
    public class ExportQueue
    {
        public int ExportQueueId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ExportId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(450)]
        public string RequestedBy { get; set; } = string.Empty;
        
        public DateTime RequestedDate { get; set; } = DateTime.UtcNow;
        public DateTime? StartedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        
        public ExportStatus Status { get; set; } = ExportStatus.Pending;
        
        public int Priority { get; set; } = 1; // 1 = highest priority
        public int RetryCount { get; set; } = 0;
        public int MaxRetries { get; set; } = 3;
        
        // Export request configuration (JSON)
        [Required]
        public string RequestJson { get; set; } = string.Empty;
        
        // Progress tracking
        public int ProgressPercentage { get; set; } = 0;
        [MaxLength(500)]
        public string? CurrentStep { get; set; }
        
        // Error information
        public string? ErrorDetails { get; set; }
        
        // Resource usage tracking
        public long? MemoryUsageBytes { get; set; }
        public TimeSpan? ProcessingTime { get; set; }
        
        // Navigation properties
        public ApplicationUser? RequestedByUser { get; set; }
        public DataExportHistory? ExportHistory { get; set; }
    }

    // Export access log for audit purposes
    public class ExportAccessLog
    {
        public int ExportAccessLogId { get; set; }
        
        public int DataExportHistoryId { get; set; }
        
        [Required]
        [MaxLength(450)]
        public string AccessedBy { get; set; } = string.Empty;
        
        public DateTime AccessDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        [MaxLength(50)]
        public string AccessType { get; set; } = string.Empty; // Download, View, Delete, etc.
        
        [MaxLength(45)] // IPv6 address length
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        public bool IsSuccessful { get; set; } = true;
        
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }
        
        // Navigation properties
        public DataExportHistory? DataExportHistory { get; set; }
        public ApplicationUser? AccessedByUser { get; set; }
    }

    // Export statistics for dashboard
    public class ExportStatistics
    {
        public int ExportStatisticsId { get; set; }
        
        public DateTime StatisticDate { get; set; } = DateTime.UtcNow.Date;
        
        public int TotalExports { get; set; }
        public long TotalSizeBytes { get; set; }
        
        // Export counts by type
        public int TaxReturnExports { get; set; }
        public int PaymentExports { get; set; }
        public int ClientExports { get; set; }
        public int ComplianceReports { get; set; }
        public int ActivityLogExports { get; set; }
        public int DocumentExports { get; set; }
        public int PenaltyExports { get; set; }
        public int ComprehensiveExports { get; set; }
        
        // Export counts by format
        public int ExcelExports { get; set; }
        public int CsvExports { get; set; }
        public int PdfExports { get; set; }
        public int JsonExports { get; set; }
        public int XmlExports { get; set; }
        
        public int FailedExports { get; set; }
        public int ExpiredExports { get; set; }
        
        public long AverageFileSizeBytes { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}