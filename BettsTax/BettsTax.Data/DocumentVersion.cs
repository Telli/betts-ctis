namespace BettsTax.Data
{
    public class DocumentVersion
    {
        public int DocumentVersionId { get; set; }
        public int DocumentId { get; set; }
        public int VersionNumber { get; set; }

        // File metadata for this version
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string StoragePath { get; set; } = string.Empty;
        public string? Checksum { get; set; }

        // Audit
        public string? UploadedById { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Soft delete for retention
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedById { get; set; }

        // Navigation
        public Document? Document { get; set; }
        public ApplicationUser? UploadedBy { get; set; }
        public ApplicationUser? DeletedBy { get; set; }
    }
}
