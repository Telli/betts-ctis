using Microsoft.AspNetCore.Identity;

namespace BettsTax.Data
{
    // Extend IdentityUser with basic profile fields – more can be added later
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginDate { get; set; }

        // Enrollment-related properties
        public bool EmailVerified { get; set; } = false;
        public string? EmailVerificationToken { get; set; }
        public DateTime? EmailVerificationSentDate { get; set; }
        public DateTime? RegistrationCompletedDate { get; set; }
        public RegistrationSource RegistrationSource { get; set; } = RegistrationSource.SelfRegistration;

        // Navigation properties for client relationships
        public Client? ClientProfile { get; set; }
        public ICollection<Client> AssignedClients { get; set; } = new List<Client>();
        public ICollection<TaxFiling> SubmittedTaxFilings { get; set; } = new List<TaxFiling>();
        public ICollection<TaxFiling> ReviewedTaxFilings { get; set; } = new List<TaxFiling>();
        public ICollection<Document> UploadedDocuments { get; set; } = new List<Document>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public ICollection<ClientInvitation> SentInvitations { get; set; } = new List<ClientInvitation>();

        // Associate permission navigation properties
        public ICollection<AssociateClientPermission> AssociatePermissions { get; set; } = new List<AssociateClientPermission>();
        public ICollection<AssociateClientPermission> GrantedPermissions { get; set; } = new List<AssociateClientPermission>();
        public ICollection<AssociatePermissionTemplate> CreatedTemplates { get; set; } = new List<AssociatePermissionTemplate>();
        public ICollection<OnBehalfAction> OnBehalfActions { get; set; } = new List<OnBehalfAction>();
        public ICollection<AssociatePermissionAuditLog> PermissionAuditLogs { get; set; } = new List<AssociatePermissionAuditLog>();
        public ICollection<AssociatePermissionAuditLog> AdminPermissionChanges { get; set; } = new List<AssociatePermissionAuditLog>();
        
        // Document sharing navigation properties
        public ICollection<DocumentShare> SharedDocuments { get; set; } = new List<DocumentShare>();
        public ICollection<DocumentShare> ReceivedDocuments { get; set; } = new List<DocumentShare>();

        // Associate delegation navigation properties
        public ICollection<TaxFiling> CreatedTaxFilings { get; set; } = new List<TaxFiling>();
        public ICollection<TaxFiling> ModifiedTaxFilings { get; set; } = new List<TaxFiling>();
        public ICollection<Payment> ProcessedPayments { get; set; } = new List<Payment>();
        public ICollection<Payment> AssociateApprovedPayments { get; set; } = new List<Payment>();
        public ICollection<Document> AssociateUploadedDocuments { get; set; } = new List<Document>();
    }
}
