using BettsTax.Data;

namespace BettsTax.Core.DTOs
{
    public class ClientDto
    {
        public int ClientId { get; set; }
        public string ClientNumber { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public ClientType ClientType { get; set; }
        public TaxpayerCategory TaxpayerCategory { get; set; }
        public decimal AnnualTurnover { get; set; }
        public string? TIN { get; set; }
        public ClientStatus Status { get; set; }
        
        // For backward compatibility with tests
        public string FirstName => ContactPerson.Split(' ').FirstOrDefault() ?? "";
        public string LastName => string.Join(" ", ContactPerson.Split(' ').Skip(1)) ?? "";
    }

    public class UpdateClientDto
    {
        public string? BusinessName { get; set; }
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public decimal? AnnualTurnover { get; set; }
        public string? TIN { get; set; }
    }
}
