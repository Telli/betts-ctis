using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data
{
    public class FilingSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TaxFilingId { get; set; }

        [ForeignKey("TaxFilingId")]
        public virtual TaxFiling TaxFiling { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Taxable { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}

