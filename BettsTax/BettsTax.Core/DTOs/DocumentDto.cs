using BettsTax.Data;

namespace BettsTax.Core.DTOs
{
    public class DocumentDto
    {
        public int DocumentId { get; set; }
        public int ClientId { get; set; }
        public int? TaxYearId { get; set; }
        public int? TaxFilingId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public DocumentCategory Category { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        
        // For backward compatibility with tests
        public string FileName => OriginalFileName;
    }
}
