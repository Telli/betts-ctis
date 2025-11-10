using BettsTax.Core.DTOs;
using BettsTax.Data;

namespace BettsTax.Core.Services
{
    /// <summary>
    /// Core interface for all accounting system integrations
    /// </summary>
    public interface IAccountingIntegrationService
    {
        /// <summary>
        /// Gets the name of the accounting system (e.g., "QuickBooks", "Xero")
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Authenticates with the accounting system using OAuth
        /// </summary>
        Task<AccountingAuthResult> AuthenticateAsync(string clientId, string redirectUri, string state);

        /// <summary>
        /// Completes OAuth authentication and stores tokens
        /// </summary>
        Task<AccountingAuthResult> CompleteAuthenticationAsync(string authCode, string state, string clientId);

        /// <summary>
        /// Tests the connection to the accounting system
        /// </summary>
        Task<AccountingConnectionResult> TestConnectionAsync(int clientId);

        /// <summary>
        /// Syncs payment data to the accounting system
        /// </summary>
        Task<AccountingSyncResult> SyncPaymentsAsync(int clientId, List<Payment> payments);

        /// <summary>
        /// Syncs tax filing data to the accounting system
        /// </summary>
        Task<AccountingSyncResult> SyncTaxFilingsAsync(int clientId, List<TaxFiling> taxFilings);

        /// <summary>
        /// Imports financial data from the accounting system
        /// </summary>
        Task<AccountingImportResult> ImportFinancialDataAsync(int clientId, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Gets account mapping configuration for the client
        /// </summary>
        Task<AccountingMappingResult> GetAccountMappingAsync(int clientId);

        /// <summary>
        /// Updates account mapping configuration
        /// </summary>
        Task<AccountingMappingResult> UpdateAccountMappingAsync(int clientId, AccountMappingDto mappingConfig);

        /// <summary>
        /// Gets sync history for the client
        /// </summary>
        Task<List<AccountingSyncHistoryDto>> GetSyncHistoryAsync(int clientId, int page = 1, int pageSize = 50);
    }
}