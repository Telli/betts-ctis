namespace BettsTax.Core.DTOs
{
    /// <summary>
    /// Result of accounting system authentication
    /// </summary>
    public class AccountingAuthResult
    {
        public bool IsSuccess { get; set; }
        public string? AuthUrl { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Scopes { get; set; } = new();
    }

    /// <summary>
    /// Result of connection test to accounting system
    /// </summary>
    public class AccountingConnectionResult
    {
        public bool IsConnected { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyId { get; set; }
        public string? LastSyncDate { get; set; }
        public string? ErrorMessage { get; set; }
        public AccountingSystemStatus Status { get; set; }
    }

    /// <summary>
    /// Result of data synchronization operation
    /// </summary>
    public class AccountingSyncResult
    {
        public bool IsSuccess { get; set; }
        public int RecordsProcessed { get; set; }
        public int RecordsSucceeded { get; set; }
        public int RecordsFailed { get; set; }
        public DateTime SyncTimestamp { get; set; }
        public string? ErrorMessage { get; set; }
        public List<AccountingSyncError> Errors { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Individual sync error details
    /// </summary>
    public class AccountingSyncError
    {
        public string RecordId { get; set; } = string.Empty;
        public string RecordType { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, object> RecordData { get; set; } = new();
    }

    /// <summary>
    /// Result of importing data from accounting system
    /// </summary>
    public class AccountingImportResult
    {
        public bool IsSuccess { get; set; }
        public List<AccountingTransactionDto> Transactions { get; set; } = new();
        public List<AccountingCustomerDto> Customers { get; set; } = new();
        public List<AccountingAccountDto> Accounts { get; set; } = new();
        public DateTime ImportTimestamp { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Account mapping configuration result
    /// </summary>
    public class AccountingMappingResult
    {
        public bool IsSuccess { get; set; }
        public AccountMappingDto? Mapping { get; set; }
        public List<AccountingAccountDto> AvailableAccounts { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Account mapping configuration
    /// </summary>
    public class AccountMappingDto
    {
        public int ClientId { get; set; }
        public string AccountingSystemName { get; set; } = string.Empty;
        public string TaxPayableAccountId { get; set; } = string.Empty;
        public string TaxExpenseAccountId { get; set; } = string.Empty;
        public string BankAccountId { get; set; } = string.Empty;
        public string RevenueAccountId { get; set; } = string.Empty;
        public string PenaltyAccountId { get; set; } = string.Empty;
        public string InterestAccountId { get; set; } = string.Empty;
        public Dictionary<string, string> CustomMappings { get; set; } = new();
        public bool AutoSync { get; set; }
        public int SyncFrequencyHours { get; set; } = 24;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Accounting system transaction data
    /// </summary>
    public class AccountingTransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? Reference { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Accounting system customer data
    /// </summary>
    public class AccountingCustomerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? TaxId { get; set; }
        public AccountingAddressDto? Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public Dictionary<string, object> CustomFields { get; set; } = new();
    }

    /// <summary>
    /// Accounting system account (chart of accounts)
    /// </summary>
    public class AccountingAccountDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? ParentId { get; set; }
        public string? Code { get; set; }
        public bool IsActive { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "SLL";
    }

    /// <summary>
    /// Address information from accounting system
    /// </summary>
    public class AccountingAddressDto
    {
        public string? Street1 { get; set; }
        public string? Street2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
    }

    /// <summary>
    /// Sync history record
    /// </summary>
    public class AccountingSyncHistoryDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string AccountingSystem { get; set; } = string.Empty;
        public string SyncType { get; set; } = string.Empty;
        public DateTime SyncTimestamp { get; set; }
        public bool IsSuccess { get; set; }
        public int RecordsProcessed { get; set; }
        public string? ErrorMessage { get; set; }
        public string? InitiatedBy { get; set; }
        public Dictionary<string, object> SyncDetails { get; set; } = new();
    }

    /// <summary>
    /// Accounting system connection configuration
    /// </summary>
    public class AccountingConnectionDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string AccountingSystem { get; set; } = string.Empty;
        public string? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public bool IsActive { get; set; }
        public DateTime ConnectedAt { get; set; }
        public DateTime? LastSyncAt { get; set; }
        public string? AccessToken { get; set; } // Encrypted
        public string? RefreshToken { get; set; } // Encrypted
        public DateTime? TokenExpiresAt { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    /// <summary>
    /// Status of accounting system connection
    /// </summary>
    public enum AccountingSystemStatus
    {
        NotConnected,
        Connected,
        TokenExpired,
        AuthenticationError,
        ConnectionError,
        Disabled
    }

    /// <summary>
    /// Supported accounting systems
    /// </summary>
    public static class AccountingSystems
    {
        public const string QuickBooks = "QuickBooks";
        public const string Xero = "Xero";
        public const string Sage = "Sage";
        public const string Wave = "Wave";
        public const string FreshBooks = "FreshBooks";
    }
}