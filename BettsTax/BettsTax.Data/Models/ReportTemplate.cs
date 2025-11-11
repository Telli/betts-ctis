using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data.Models
{
    public class ReportTemplate
    {
        [Key]
        public int ReportTemplateId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ReportType { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Icon { get; set; } = string.Empty;

        public int EstimatedDurationSeconds { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// JSON-serialized list of supported formats (e.g., ["PDF", "Excel", "CSV"])
        /// </summary>
        public string SupportedFormats { get; set; } = "[]";

        /// <summary>
        /// JSON-serialized list of features (e.g., ["Compliance scoring", "Deadline tracking"])
        /// </summary>
        public string Features { get; set; } = "[]";

        /// <summary>
        /// JSON-serialized list of required fields (e.g., ["clientId"])
        /// </summary>
        public string RequiredFields { get; set; } = "[]";

        /// <summary>
        /// JSON-serialized array of parameter definitions
        /// </summary>
        public string Parameters { get; set; } = "[]";

        /// <summary>
        /// JSON-serialized default values for parameters
        /// </summary>
        public string DefaultParameterValues { get; set; } = "{}";

        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? CreatedByUserId { get; set; }

        [MaxLength(450)]
        public string? UpdatedByUserId { get; set; }

        // Navigation properties
        [ForeignKey(nameof(CreatedByUserId))]
        public ApplicationUser? CreatedBy { get; set; }

        [ForeignKey(nameof(UpdatedByUserId))]
        public ApplicationUser? UpdatedBy { get; set; }
    }
}
