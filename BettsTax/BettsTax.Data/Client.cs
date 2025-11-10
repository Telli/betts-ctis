namespace BettsTax.Data
{
    public enum ClientType { Individual, Partnership, Corporation, NGO }
    public enum TaxpayerCategory { Large, Medium, Small, Micro }
    public enum ClientStatus { Active, Inactive, Suspended }

    public class Client
    {
        public int ClientId { get; set; }
        public int Id { get; set; } // Added for compatibility
        public string? UserId { get; set; } // FK to ApplicationUser (optional)
        public string ClientNumber { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty; // Added for compatibility
        public string Name { get; set; } = string.Empty; // Added for compatibility
        public string FirstName { get; set; } = string.Empty; // Added for compatibility
        public string LastName { get; set; } = string.Empty; // Added for compatibility
        public string CompanyRegistrationNumber { get; set; } = string.Empty; // Added for compatibility
        public string ContactPerson { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public ClientType ClientType { get; set; }
        public TaxpayerCategory TaxpayerCategory { get; set; }
        public decimal AnnualTurnover { get; set; }
        public string? TIN { get; set; }
        public string? AssignedAssociateId { get; set; } // optional
        public ClientStatus Status { get; set; } = ClientStatus.Active;
        public bool IsActive { get; set; } = true; // Added for compatibility
        public double ComplianceScore { get; set; } = 0; // Added for compatibility
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime DateCreated { get; set; } = DateTime.UtcNow; // Added for compatibility
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
        public ApplicationUser? AssignedAssociate { get; set; }
    }
}
