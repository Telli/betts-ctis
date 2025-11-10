using System;

namespace BettsTax.Core.DTOs
{
    public class DocumentVersionDto
    {
        public int DocumentVersionId { get; set; }
        public int DocumentId { get; set; }
        public int VersionNumber { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime UploadedAt { get; set; }
        public string? UploadedById { get; set; }
        public bool IsDeleted { get; set; }
    }
}
