namespace BettsTax.Data
{
    public enum DocumentVerificationStatus
    {
        NotRequested,     // Document not yet requested from client
        Requested,        // Document requested, awaiting submission
        Submitted,        // Document uploaded by client, pending review
        UnderReview,      // Being reviewed by Betts Firm staff
        Rejected,         // Document rejected, resubmission required
        Verified,         // Document approved and verified
        Filed             // Document included in tax filing
    }

    public class DocumentVerification
    {
        public int DocumentVerificationId { get; set; }
        public int DocumentId { get; set; }
        public DocumentVerificationStatus Status { get; set; } = DocumentVerificationStatus.Submitted;
        
        // Verification details
        public string? ReviewedById { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public string? ReviewNotes { get; set; }
        public string? RejectionReason { get; set; }
        
        // Verification history tracking
        public DateTime StatusChangedDate { get; set; } = DateTime.UtcNow;
        public string? StatusChangedById { get; set; }
        
        // Additional verification metadata
        public bool IsRequiredDocument { get; set; } = true;
        public string? DocumentRequirementType { get; set; } // e.g., "PaySlip", "BankStatement", "TaxClearance"
        public int? TaxFilingId { get; set; } // Link to specific tax filing if applicable
        
        // Navigation properties
        public Document? Document { get; set; }
        public ApplicationUser? ReviewedBy { get; set; }
        public ApplicationUser? StatusChangedBy { get; set; }
        public TaxFiling? TaxFiling { get; set; }
    }

    // Track the history of verification status changes
    public class DocumentVerificationHistory
    {
        public int DocumentVerificationHistoryId { get; set; }
        public int DocumentId { get; set; }
        public DocumentVerificationStatus OldStatus { get; set; }
        public DocumentVerificationStatus NewStatus { get; set; }
        public string? ChangedById { get; set; }
        public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        
        // Navigation properties
        public Document? Document { get; set; }
        public ApplicationUser? ChangedBy { get; set; }
    }
}