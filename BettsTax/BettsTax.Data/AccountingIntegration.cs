using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data
{
    /// <summary>
    /// Stores accounting system connection configurations for clients
    /// </summary>
    [Table("AccountingConnections")]
    public class AccountingConnection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        [MaxLength(50)]
        public string AccountingSystem { get; set; } = string.Empty; // QuickBooks, Xero, etc.

        [MaxLength(100)]
        public string? CompanyId { get; set; } // External company ID

        [MaxLength(200)]
        public string? CompanyName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastSyncAt { get; set; }

        [MaxLength(1000)]
        public string? EncryptedAccessToken { get; set; } // OAuth access token (encrypted)

        [MaxLength(1000)]
        public string? EncryptedRefreshToken { get; set; } // OAuth refresh token (encrypted)

        public DateTime? TokenExpiresAt { get; set; }

        [Column(TypeName = "TEXT")]
        public string? SettingsJson { get; set; } // JSON configuration settings

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ClientId")]
        public virtual Client Client { get; set; } = null!;

        public virtual ICollection<AccountingMapping> AccountMappings { get; set; } = new List<AccountingMapping>();
        public virtual ICollection<AccountingSyncHistory> SyncHistory { get; set; } = new List<AccountingSyncHistory>();
    }

    /// <summary>
    /// Stores account mapping configurations between CTIS and accounting systems
    /// </summary>
    [Table("AccountingMappings")]
    public class AccountingMapping
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AccountingConnectionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string AccountType { get; set; } = string.Empty; // TaxPayable, TaxExpense, Bank, etc.

        [Required]
        [MaxLength(100)]
        public string ExternalAccountId { get; set; } = string.Empty; // Account ID in external system

        [MaxLength(200)]
        public string? ExternalAccountName { get; set; }

        [MaxLength(50)]
        public string? ExternalAccountCode { get; set; }

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "TEXT")]
        public string? MappingRulesJson { get; set; } // JSON rules for complex mappings

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("AccountingConnectionId")]
        public virtual AccountingConnection AccountingConnection { get; set; } = null!;
    }

    /// <summary>
    /// Tracks synchronization history between CTIS and accounting systems
    /// </summary>
    [Table("AccountingSyncHistory")]
    public class AccountingSyncHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AccountingConnectionId { get; set; }

        [Required]
        [MaxLength(50)]
        public string SyncType { get; set; } = string.Empty; // Payments, TaxFilings, Import, etc.

        [Required]
        [MaxLength(20)]
        public string Direction { get; set; } = string.Empty; // Export, Import, Bidirectional

        public DateTime SyncStartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SyncCompletedAt { get; set; }

        public bool IsSuccess { get; set; }

        public int RecordsProcessed { get; set; }

        public int RecordsSucceeded { get; set; }

        public int RecordsFailed { get; set; }

        [Column(TypeName = "TEXT")]
        public string? ErrorMessage { get; set; }

        [Column(TypeName = "TEXT")]
        public string? ErrorDetailsJson { get; set; } // JSON array of detailed errors

        [MaxLength(100)]
        public string? InitiatedBy { get; set; } // User ID or "System"

        [Column(TypeName = "TEXT")]
        public string? SyncDetailsJson { get; set; } // Additional sync metadata

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("AccountingConnectionId")]
        public virtual AccountingConnection AccountingConnection { get; set; } = null!;
    }

    /// <summary>
    /// Stores external transaction mappings for reconciliation
    /// </summary>
    [Table("AccountingTransactionMappings")]
    public class AccountingTransactionMapping
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AccountingConnectionId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CtisRecordType { get; set; } = string.Empty; // Payment, TaxFiling

        [Required]
        public int CtisRecordId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ExternalTransactionId { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? ExternalTransactionType { get; set; }

        public decimal Amount { get; set; }

        public DateTime TransactionDate { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsSynced { get; set; } = true;

        public DateTime? LastSyncedAt { get; set; }

        [Column(TypeName = "TEXT")]
        public string? ExternalDataJson { get; set; } // Full external transaction data

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("AccountingConnectionId")]
        public virtual AccountingConnection AccountingConnection { get; set; } = null!;
    }
}