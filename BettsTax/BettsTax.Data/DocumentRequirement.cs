using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data
{
    // Defines what documents are required for specific tax types and taxpayer categories
    public class DocumentRequirement
    {
        public int DocumentRequirementId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string RequirementCode { get; set; } = string.Empty; // e.g., "PAYSLIP", "BANK_STATEMENT"
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty; // e.g., "Employment Pay Slips"
        
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty; // Detailed description of requirement
        
        public TaxType? ApplicableTaxType { get; set; } // Which tax type this applies to
        public TaxpayerCategory? ApplicableTaxpayerCategory { get; set; } // Which taxpayer category
        
        public bool IsRequired { get; set; } = true; // Required vs Optional
        public int DisplayOrder { get; set; } = 0; // Order in checklist
        
        // File validation rules
        [MaxLength(200)]
        public string AcceptedFormats { get; set; } = "pdf,jpg,jpeg,png,doc,docx"; // Comma-separated
        public long MaxFileSizeInBytes { get; set; } = 10485760; // 10MB default
        public int MinimumQuantity { get; set; } = 1; // Minimum number of documents needed
        public int MaximumQuantity { get; set; } = 10; // Maximum allowed
        
        // Period requirements
        public int? RequiredMonthsOfData { get; set; } // e.g., 12 months of bank statements
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }

    // Links a document requirement to a specific client's tax filing
    public class ClientDocumentRequirement
    {
        public int ClientDocumentRequirementId { get; set; }
        public int ClientId { get; set; }
        public int TaxFilingId { get; set; }
        public int DocumentRequirementId { get; set; }
        
        // Status tracking
        public DocumentVerificationStatus Status { get; set; } = DocumentVerificationStatus.NotRequested;
        public DateTime? RequestedDate { get; set; }
        public string? RequestedById { get; set; }
        public DateTime? FulfilledDate { get; set; }
        
        // Documents submitted for this requirement
        public string DocumentIds { get; set; } = string.Empty; // Comma-separated list of DocumentIds
        public int DocumentCount { get; set; } = 0;
        
        // Navigation properties
        public Client? Client { get; set; }
        public TaxFiling? TaxFiling { get; set; }
        public DocumentRequirement? DocumentRequirement { get; set; }
        public ApplicationUser? RequestedBy { get; set; }
    }
}