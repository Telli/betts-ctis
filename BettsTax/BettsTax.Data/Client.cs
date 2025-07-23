namespace BettsTax.Data
{
    public enum ClientType { Individual, Partnership, Corporation, NGO }
    public enum TaxpayerCategory { Large, Medium, Small, Micro }
    public enum ClientStatus { Active, Inactive, Suspended }

    public class Client
    {
        public int ClientId { get; set; }
        public string UserId { get; set; } = string.Empty; // FK to ApplicationUser
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
        public string AssignedAssociateId { get; set; } = string.Empty;
        public ClientStatus Status { get; set; } = ClientStatus.Active;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
        public ApplicationUser? AssignedAssociate { get; set; }
    }
}
