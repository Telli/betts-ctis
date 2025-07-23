using BettsTax.Data;
using System.ComponentModel.DataAnnotations;

namespace BettsTax.Core.DTOs
{
    public enum ExportFormat
    {
        Excel,
        CSV,
        PDF,
        JSON,
        XML
    }

    public enum ExportType
    {
        TaxReturns,
        Payments,
        Clients,
        ComplianceReport,
        ActivityLog,
        Documents,
        Messages,
        Penalties,
        Comprehensive
    }

    public class ExportRequestDto
    {
        [Required]
        public ExportType ExportType { get; set; }
        
        [Required]
        public ExportFormat Format { get; set; }
        
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        public List<int> ClientIds { get; set; } = new();
        public List<int> TaxYearIds { get; set; } = new();
        public List<TaxType> TaxTypes { get; set; } = new();
        
        public bool IncludeDocuments { get; set; } = false;
        public bool IncludeMessages { get; set; } = false;
        public bool IncludeActivityLog { get; set; } = false;
        public bool IncludePenalties { get; set; } = false;
        
        public string? FileName { get; set; }
        public string? Description { get; set; }
        
        // Filters
        public PaymentStatus? PaymentStatus { get; set; }
        public FilingStatus? FilingStatus { get; set; }
        public ComplianceStatus? ComplianceStatus { get; set; }
        
        // Security
        public bool PasswordProtected { get; set; } = false;
        public string? Password { get; set; }
    }

    public class ExportResultDto
    {
        public string ExportId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public ExportFormat Format { get; set; }
        public ExportType ExportType { get; set; }
        public long FileSizeBytes { get; set; }
        public int RecordCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? DownloadUrl { get; set; }
        public bool IsPasswordProtected { get; set; }
        public string? Description { get; set; }
        public ExportMetadataDto Metadata { get; set; } = new();
    }

    public class ExportMetadataDto
    {
        public DateTime GeneratedDate { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public DateTime? DataStartDate { get; set; }
        public DateTime? DataEndDate { get; set; }
        public List<string> IncludedFields { get; set; } = new();
        public Dictionary<string, object> Filters { get; set; } = new();
        public List<string> IncludedTables { get; set; } = new();
        public string SystemVersion { get; set; } = string.Empty;
        public string ExportVersion { get; set; } = "1.0";
    }

    // Tax Returns Export
    public class TaxReturnExportDto
    {
        public int TaxFilingId { get; set; }
        public string ClientNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string TIN { get; set; } = string.Empty;
        public int TaxYear { get; set; }
        public string TaxType { get; set; } = string.Empty;
        public string FilingStatus { get; set; } = string.Empty;
        public decimal TaxLiability { get; set; }
        public decimal TaxPaid { get; set; }
        public decimal OutstandingBalance { get; set; }
        public DateTime FilingDueDate { get; set; }
        public DateTime? FiledDate { get; set; }
        public DateTime? PaymentDueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdated { get; set; }
        
        // Additional fields for comprehensive export
        public string TaxpayerCategory { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;
        public bool HasPenalties { get; set; }
        public decimal TotalPenalties { get; set; }
        public int DaysOverdue { get; set; }
        public string ComplianceStatus { get; set; } = string.Empty;
    }

    // Payment Export
    public class PaymentExportDto
    {
        public int PaymentId { get; set; }
        public string ClientNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string TIN { get; set; } = string.Empty;
        public int TaxYear { get; set; }
        public string TaxType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string? TransactionId { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // Additional fields
        public string PaymentProvider { get; set; } = string.Empty;
        public decimal? Fee { get; set; }
        public string Currency { get; set; } = "SLE";
        public bool IsReconciled { get; set; }
        public DateTime? ReconciliationDate { get; set; }
    }

    // Client Export
    public class ClientExportDto
    {
        public int ClientId { get; set; }
        public string ClientNumber { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string TIN { get; set; } = string.Empty;
        public string TaxpayerCategory { get; set; } = string.Empty;
        public string BusinessType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public string ClientStatus { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime LastLoginDate { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // Summary fields
        public int TotalTaxFilings { get; set; }
        public decimal TotalTaxPaid { get; set; }
        public decimal OutstandingBalance { get; set; }
        public string ComplianceStatus { get; set; } = string.Empty;
        public decimal ComplianceScore { get; set; }
        public int UnresolvedAlerts { get; set; }
        public int PendingActions { get; set; }
    }

    // Compliance Report Export
    public class ComplianceReportExportDto
    {
        public string ClientNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string TIN { get; set; } = string.Empty;
        public int TaxYear { get; set; }
        public string TaxType { get; set; } = string.Empty;
        public string ComplianceStatus { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public decimal ComplianceScore { get; set; }
        public bool IsFilingRequired { get; set; }
        public bool IsPaymentRequired { get; set; }
        public bool IsFilingComplete { get; set; }
        public bool IsPaymentComplete { get; set; }
        public bool IsDocumentationComplete { get; set; }
        public DateTime FilingDueDate { get; set; }
        public DateTime PaymentDueDate { get; set; }
        public DateTime? FiledDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public int DaysOverdueForFiling { get; set; }
        public int DaysOverdueForPayment { get; set; }
        public decimal TaxLiability { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal OutstandingBalance { get; set; }
        public decimal TotalPenaltiesOwed { get; set; }
        public decimal TotalPenaltiesPaid { get; set; }
        public decimal OutstandingPenalties { get; set; }
        public int ActiveAlertsCount { get; set; }
        public int CriticalAlertsCount { get; set; }
        public int PendingActionsCount { get; set; }
        public int OverdueActionsCount { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    // Activity Log Export
    public class ActivityLogExportDto
    {
        public int ActivityId { get; set; }
        public string ClientNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ActivityDate { get; set; }
        public string? PerformedBy { get; set; }
        public string? PerformedByRole { get; set; }
        public string? RelatedEntity { get; set; }
        public int? RelatedEntityId { get; set; }
        public string? Metadata { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    // Document Export
    public class DocumentExportDto
    {
        public int DocumentId { get; set; }
        public string ClientNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string DocumentName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public DateTime? VerifiedDate { get; set; }
        public string? VerifiedBy { get; set; }
        public string? Notes { get; set; }
        public int? TaxYear { get; set; }
        public string? TaxType { get; set; }
        public string? FilePath { get; set; }
        public bool IsRequired { get; set; }
        public DateTime? RequiredByDate { get; set; }
    }

    // Penalty Export
    public class PenaltyExportDto
    {
        public int PenaltyId { get; set; }
        public string ClientNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string TIN { get; set; } = string.Empty;
        public int TaxYear { get; set; }
        public string TaxType { get; set; } = string.Empty;
        public string PenaltyType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal OutstandingAmount { get; set; }
        public decimal? PenaltyRate { get; set; }
        public decimal? BaseAmount { get; set; }
        public int? DaysOverdue { get; set; }
        public bool IsPaid { get; set; }
        public bool IsWaived { get; set; }
        public string? WaiverReason { get; set; }
        public DateTime PenaltyDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime? WaivedDate { get; set; }
        public string? CalculationDetails { get; set; }
        public string? LegalReference { get; set; }
    }

    // Export History
    public class ExportHistoryDto
    {
        public int ExportHistoryId { get; set; }
        public string ExportId { get; set; } = string.Empty;
        public string ExportType { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int RecordCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime? DownloadedDate { get; set; }
        public int DownloadCount { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPasswordProtected { get; set; }
        public string? Filters { get; set; }
    }

    // Bulk Export Request for multiple export types
    public class BulkExportRequestDto
    {
        public List<ExportRequestDto> ExportRequests { get; set; } = new();
        public string BulkExportName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool CompressAsZip { get; set; } = true;
        public bool PasswordProtected { get; set; } = false;
        public string? Password { get; set; }
    }

    public class BulkExportResultDto
    {
        public string BulkExportId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long TotalSizeBytes { get; set; }
        public int TotalFiles { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? DownloadUrl { get; set; }
        public List<ExportResultDto> Exports { get; set; } = new();
        public string? Description { get; set; }
    }
}