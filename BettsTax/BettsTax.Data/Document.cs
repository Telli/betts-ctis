namespace BettsTax.Data
{
    public enum DocumentCategory { TaxReturn, FinancialStatement, Receipt, Invoice, PaymentEvidence, BankStatement, Other }

    public class Document
    {
        public int DocumentId { get; set; }
        public int ClientId { get; set; }
        public int? TaxYearId { get; set; }
        public int? TaxFilingId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public DocumentCategory Category { get; set; } = DocumentCategory.Other;
        public string DocumentType { get; set; } = string.Empty; // Added for compatibility
        public DocumentStatus Status { get; set; } = DocumentStatus.Pending; // Added for compatibility
        public string Description { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string? UploadedById { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public int CurrentVersionNumber { get; set; } = 0;

        // Additional properties for DbSeeder compatibility
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DocumentVerificationStatus VerificationStatus { get; set; } = DocumentVerificationStatus.NotRequested;
        public string? VerifiedById { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Associate delegation fields
        public string? UploadedByAssociateId { get; set; }
        public ApplicationUser? UploadedByAssociate { get; set; }
        public bool IsUploadedOnBehalf { get; set; } = false;
        public DateTime? OnBehalfUploadDate { get; set; }

        // Navigation properties
        public Client? Client { get; set; }
        public TaxYear? TaxYear { get; set; }
        public TaxFiling? TaxFiling { get; set; }
        public ApplicationUser? UploadedBy { get; set; }
        public List<DocumentShare> SharedWith { get; set; } = new();
        public List<DocumentVersion> Versions { get; set; } = new();
        
        // Verification tracking
        public DocumentVerification? DocumentVerification { get; set; }
    }
}

