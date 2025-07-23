using BettsTax.Data;

namespace BettsTax.Core.DTOs
{
    public class DocumentVerificationDto
    {
        public int DocumentVerificationId { get; set; }
        public int DocumentId { get; set; }
        public DocumentVerificationStatus Status { get; set; }
        public string? ReviewedById { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public string? ReviewNotes { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime StatusChangedDate { get; set; }
        public string? StatusChangedById { get; set; }
        public string? StatusChangedByName { get; set; }
        public bool IsRequiredDocument { get; set; }
        public string? DocumentRequirementType { get; set; }
        public int? TaxFilingId { get; set; }
        
        // Document details
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public DocumentCategory Category { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string? UploadedByName { get; set; }
        
        // Client details
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientNumber { get; set; } = string.Empty;
    }

    public class DocumentVerificationCreateDto
    {
        public int DocumentId { get; set; }
        public bool IsRequiredDocument { get; set; } = true;
        public string? DocumentRequirementType { get; set; }
        public int? TaxFilingId { get; set; }
    }

    public class DocumentVerificationUpdateDto
    {
        public DocumentVerificationStatus Status { get; set; }
        public string? ReviewNotes { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class DocumentVerificationHistoryDto
    {
        public int DocumentVerificationHistoryId { get; set; }
        public int DocumentId { get; set; }
        public DocumentVerificationStatus OldStatus { get; set; }
        public DocumentVerificationStatus NewStatus { get; set; }
        public string? ChangedById { get; set; }
        public string? ChangedByName { get; set; }
        public DateTime ChangedDate { get; set; }
        public string? Notes { get; set; }
    }

    public class DocumentRequirementDto
    {
        public int DocumentRequirementId { get; set; }
        public string RequirementCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TaxType? ApplicableTaxType { get; set; }
        public TaxpayerCategory? ApplicableTaxpayerCategory { get; set; }
        public bool IsRequired { get; set; }
        public int DisplayOrder { get; set; }
        public string AcceptedFormats { get; set; } = string.Empty;
        public long MaxFileSizeInBytes { get; set; }
        public int MinimumQuantity { get; set; }
        public int MaximumQuantity { get; set; }
        public int? RequiredMonthsOfData { get; set; }
    }

    public class ClientDocumentRequirementDto
    {
        public int ClientDocumentRequirementId { get; set; }
        public int ClientId { get; set; }
        public int TaxFilingId { get; set; }
        public int DocumentRequirementId { get; set; }
        public DocumentVerificationStatus Status { get; set; }
        public DateTime? RequestedDate { get; set; }
        public string? RequestedById { get; set; }
        public string? RequestedByName { get; set; }
        public DateTime? FulfilledDate { get; set; }
        public int DocumentCount { get; set; }
        
        // Document requirement details
        public DocumentRequirementDto? DocumentRequirement { get; set; }
        
        // Submitted documents
        public List<DocumentVerificationDto> SubmittedDocuments { get; set; } = new();
    }

    public class DocumentVerificationSummaryDto
    {
        public int TotalDocuments { get; set; }
        public int VerifiedDocuments { get; set; }
        public int PendingReview { get; set; }
        public int RejectedDocuments { get; set; }
        public int MissingDocuments { get; set; }
        public decimal CompletionPercentage { get; set; }
        public List<ClientDocumentRequirementDto> Requirements { get; set; } = new();
    }

    public class DocumentReviewRequestDto
    {
        public int DocumentId { get; set; }
        public bool Approved { get; set; }
        public string? ReviewNotes { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class BulkDocumentReviewDto
    {
        public List<int> DocumentIds { get; set; } = new();
        public bool Approved { get; set; }
        public string? ReviewNotes { get; set; }
    }
}