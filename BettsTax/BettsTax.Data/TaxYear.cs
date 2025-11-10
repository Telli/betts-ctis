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
        public TaxYearStatus Status { get; set; } = TaxYearStatus.Draft;
        public DateTime? FilingDeadline { get; set; }
        public DateTime? DateFiled { get; set; }

        public Client? Client { get; set; }
    }
}
