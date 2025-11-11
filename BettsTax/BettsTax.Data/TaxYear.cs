namespace BettsTax.Data
{
    public enum TaxYearStatus { Draft, Pending, Filed, Paid, Overdue }

    public class TaxYear
    {
        public int TaxYearId { get; set; }
        public int Id { get; set; } // Added for compatibility
        public int ClientId { get; set; }
        public int Year { get; set; }
        public DateTime StartDate { get; set; } // Added for compatibility
        public DateTime EndDate { get; set; } // Added for compatibility
        public DateTime DueDate { get; set; } // Added for compatibility
        public decimal? IncomeTaxOwed { get; set; }
        public decimal? TaxLiability { get; set; } // Total tax liability across all types
        public TaxYearStatus Status { get; set; } = TaxYearStatus.Draft;
        public DateTime? FilingDeadline { get; set; }
        public DateTime? DateFiled { get; set; }

        /// <summary>
        /// Computed property that maps to DateFiled for compatibility.
        /// Always synchronized - setting either property updates both.
        /// </summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public DateTime? FilingDate
        {
            get => DateFiled;
            set => DateFiled = value;
        }

        // Audit fields
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        public Client? Client { get; set; }
    }
}
