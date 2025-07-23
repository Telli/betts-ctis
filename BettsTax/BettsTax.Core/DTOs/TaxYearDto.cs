namespace BettsTax.Core.DTOs
{
    public class TaxYearDto
    {
        public int TaxYearId { get; set; }
        public int ClientId { get; set; }
        public int Year { get; set; }
        public decimal? IncomeTaxOwed { get; set; }
    }
}
