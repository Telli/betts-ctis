using BettsTax.Core.DTOs;
using BettsTax.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace BettsTax.Core.Services
{
    /// <summary>
    /// QuickBooks Online API integration service
    /// Implements OAuth 2.0 flow and data synchronization with QuickBooks
    /// </summary>
    public class QuickBooksIntegrationService : IAccountingIntegrationService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<QuickBooksIntegrationService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        // QuickBooks API configuration
        private readonly string _baseUrl;
        private readonly string _discoveryUrl;
        private readonly string _clientId;
        private readonly string _clientSecret;

        public string ProviderName => AccountingSystems.QuickBooks;

        public QuickBooksIntegrationService(
            HttpClient httpClient,
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<QuickBooksIntegrationService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _configuration = configuration;
            _logger = logger;

            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            // Load QuickBooks configuration
            _baseUrl = _configuration["QuickBooks:BaseUrl"] ?? "https://sandbox-quickbooks.api.intuit.com";
            _discoveryUrl = _configuration["QuickBooks:DiscoveryUrl"] ?? "https://appcenter.intuit.com/connect/oauth2";
            _clientId = _configuration["QuickBooks:ClientId"] ?? string.Empty;
            _clientSecret = _configuration["QuickBooks:ClientSecret"] ?? string.Empty;

            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                _logger.LogWarning("QuickBooks ClientId and ClientSecret must be configured");
            }
        }

        /// <summary>
        /// Initiates OAuth authentication flow with QuickBooks
        /// </summary>
        public async Task<AccountingAuthResult> AuthenticateAsync(string clientId, string redirectUri, string state)
        {
            try
            {
                var authUrl = $"{_discoveryUrl}?" +
                             $"client_id={_clientId}&" +
                             $"scope=com.intuit.quickbooks.accounting&" +
                             $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                             $"response_type=code&" +
                             $"access_type=offline&" +
                             $"state={state}";

                return new AccountingAuthResult
                {
                    IsSuccess = true,
                    AuthUrl = authUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating QuickBooks authentication");
                return new AccountingAuthResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Completes OAuth authentication and exchanges code for tokens
        /// </summary>
        public async Task<AccountingAuthResult> CompleteAuthenticationAsync(string authCode, string state, string redirectUri)
        {
            try
            {
                var tokenEndpoint = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";
                
                var tokenRequest = new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = authCode,
                    ["redirect_uri"] = redirectUri
                };

                var requestContent = new FormUrlEncodedContent(tokenRequest);
                
                // Add Basic authentication header
                var credentials = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

                var response = await _httpClient.PostAsync(tokenEndpoint, requestContent);
                
                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = await response.Content.ReadFromJsonAsync<QuickBooksTokenResponse>(_jsonOptions);
                    
                    if (tokenResponse != null)
                    {
                        return new AccountingAuthResult
                        {
                            IsSuccess = true,
                            AccessToken = tokenResponse.AccessToken,
                            RefreshToken = tokenResponse.RefreshToken,
                            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                            CompanyId = tokenResponse.RealmId
                        };
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("QuickBooks token exchange failed: {StatusCode} - {Content}", 
                    response.StatusCode, errorContent);

                return new AccountingAuthResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Token exchange failed: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing QuickBooks authentication");
                return new AccountingAuthResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Tests connection to QuickBooks by making a simple API call
        /// </summary>
        public async Task<AccountingConnectionResult> TestConnectionAsync(int clientId)
        {
            try
            {
                var connection = await GetActiveConnectionAsync(clientId);
                if (connection == null)
                {
                    return new AccountingConnectionResult
                    {
                        IsConnected = false,
                        Status = AccountingSystemStatus.NotConnected,
                        ErrorMessage = "No active QuickBooks connection found"
                    };
                }

                // Test connection by fetching company info
                var companyInfo = await GetCompanyInfoAsync(connection);
                if (companyInfo != null)
                {
                    return new AccountingConnectionResult
                    {
                        IsConnected = true,
                        Status = AccountingSystemStatus.Connected,
                        CompanyId = connection.CompanyId,
                        CompanyName = companyInfo.Name,
                        LastSyncDate = connection.LastSyncAt?.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                }

                return new AccountingConnectionResult
                {
                    IsConnected = false,
                    Status = AccountingSystemStatus.ConnectionError,
                    ErrorMessage = "Failed to retrieve company information"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing QuickBooks connection for client {ClientId}", clientId);
                return new AccountingConnectionResult
                {
                    IsConnected = false,
                    Status = AccountingSystemStatus.ConnectionError,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Syncs payment data to QuickBooks as journal entries
        /// </summary>
        public async Task<AccountingSyncResult> SyncPaymentsAsync(int clientId, List<Payment> payments)
        {
            var result = new AccountingSyncResult
            {
                SyncTimestamp = DateTime.UtcNow,
                RecordsProcessed = payments.Count
            };

            try
            {
                var connection = await GetActiveConnectionAsync(clientId);
                if (connection == null)
                {
                    result.ErrorMessage = "No active QuickBooks connection found";
                    return result;
                }

                var mapping = await GetAccountMappingAsync(clientId);
                if (mapping.Mapping == null)
                {
                    result.ErrorMessage = "Account mapping configuration not found";
                    return result;
                }

                foreach (var payment in payments)
                {
                    try
                    {
                        await SyncSinglePaymentAsync(connection, mapping.Mapping, payment);
                        result.RecordsSucceeded++;
                    }
                    catch (Exception ex)
                    {
                        result.RecordsFailed++;
                        result.Errors.Add(new AccountingSyncError
                        {
                            RecordId = payment.Id.ToString(),
                            RecordType = "Payment",
                            ErrorCode = "SYNC_FAILED",
                            ErrorMessage = ex.Message,
                            RecordData = new Dictionary<string, object>
                            {
                                ["PaymentId"] = payment.Id,
                                ["Amount"] = payment.Amount,
                                ["PaymentDate"] = payment.PaymentDate
                            }
                        });
                        _logger.LogError(ex, "Failed to sync payment {PaymentId} to QuickBooks", payment.Id);
                    }
                }

                result.IsSuccess = result.RecordsFailed == 0;
                
                // Record sync history
                await RecordSyncHistoryAsync(connection.Id, "Payments", "Export", result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing payments to QuickBooks for client {ClientId}", clientId);
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Syncs tax filing data to QuickBooks
        /// </summary>
        public async Task<AccountingSyncResult> SyncTaxFilingsAsync(int clientId, List<TaxFiling> taxFilings)
        {
            var result = new AccountingSyncResult
            {
                SyncTimestamp = DateTime.UtcNow,
                RecordsProcessed = taxFilings.Count
            };

            try
            {
                var connection = await GetActiveConnectionAsync(clientId);
                if (connection == null)
                {
                    result.ErrorMessage = "No active QuickBooks connection found";
                    return result;
                }

                var mapping = await GetAccountMappingAsync(clientId);
                if (mapping.Mapping == null)
                {
                    result.ErrorMessage = "Account mapping configuration not found";
                    return result;
                }

                foreach (var taxFiling in taxFilings)
                {
                    try
                    {
                        await SyncSingleTaxFilingAsync(connection, mapping.Mapping, taxFiling);
                        result.RecordsSucceeded++;
                    }
                    catch (Exception ex)
                    {
                        result.RecordsFailed++;
                        result.Errors.Add(new AccountingSyncError
                        {
                            RecordId = taxFiling.Id.ToString(),
                            RecordType = "TaxFiling",
                            ErrorCode = "SYNC_FAILED",
                            ErrorMessage = ex.Message
                        });
                        _logger.LogError(ex, "Failed to sync tax filing {TaxFilingId} to QuickBooks", taxFiling.Id);
                    }
                }

                result.IsSuccess = result.RecordsFailed == 0;
                
                // Record sync history
                await RecordSyncHistoryAsync(connection.Id, "TaxFilings", "Export", result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing tax filings to QuickBooks for client {ClientId}", clientId);
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Imports financial data from QuickBooks
        /// </summary>
        public async Task<AccountingImportResult> ImportFinancialDataAsync(int clientId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var connection = await GetActiveConnectionAsync(clientId);
                if (connection == null)
                {
                    return new AccountingImportResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "No active QuickBooks connection found"
                    };
                }

                var transactions = await GetTransactionsAsync(connection, fromDate, toDate);
                var customers = await GetCustomersAsync(connection);
                var accounts = await GetAccountsAsync(connection);

                return new AccountingImportResult
                {
                    IsSuccess = true,
                    Transactions = transactions,
                    Customers = customers,
                    Accounts = accounts,
                    ImportTimestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing data from QuickBooks for client {ClientId}", clientId);
                return new AccountingImportResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Gets current account mapping configuration
        /// </summary>
        public async Task<AccountingMappingResult> GetAccountMappingAsync(int clientId)
        {
            try
            {
                var connection = await GetActiveConnectionAsync(clientId);
                if (connection == null)
                {
                    return new AccountingMappingResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "No active QuickBooks connection found"
                    };
                }

                var mappings = await _context.AccountingMappings
                    .Where(m => m.AccountingConnectionId == connection.Id && m.IsActive)
                    .ToListAsync();

                var mapping = new AccountMappingDto
                {
                    ClientId = clientId,
                    AccountingSystemName = ProviderName,
                    CreatedAt = connection.CreatedAt,
                    UpdatedAt = connection.UpdatedAt
                };

                // Build mapping from individual mapping records
                foreach (var m in mappings)
                {
                    switch (m.AccountType.ToLower())
                    {
                        case "taxpayable":
                            mapping.TaxPayableAccountId = m.ExternalAccountId;
                            break;
                        case "taxexpense":
                            mapping.TaxExpenseAccountId = m.ExternalAccountId;
                            break;
                        case "bank":
                            mapping.BankAccountId = m.ExternalAccountId;
                            break;
                        case "revenue":
                            mapping.RevenueAccountId = m.ExternalAccountId;
                            break;
                        case "penalty":
                            mapping.PenaltyAccountId = m.ExternalAccountId;
                            break;
                        case "interest":
                            mapping.InterestAccountId = m.ExternalAccountId;
                            break;
                        default:
                            mapping.CustomMappings[m.AccountType] = m.ExternalAccountId;
                            break;
                    }
                }

                var availableAccounts = await GetAccountsAsync(connection);

                return new AccountingMappingResult
                {
                    IsSuccess = true,
                    Mapping = mapping,
                    AvailableAccounts = availableAccounts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account mapping for client {ClientId}", clientId);
                return new AccountingMappingResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Updates account mapping configuration
        /// </summary>
        public async Task<AccountingMappingResult> UpdateAccountMappingAsync(int clientId, AccountMappingDto mappingConfig)
        {
            try
            {
                var connection = await GetActiveConnectionAsync(clientId);
                if (connection == null)
                {
                    return new AccountingMappingResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "No active QuickBooks connection found"
                    };
                }

                // Clear existing mappings
                var existingMappings = await _context.AccountingMappings
                    .Where(m => m.AccountingConnectionId == connection.Id)
                    .ToListAsync();

                _context.AccountingMappings.RemoveRange(existingMappings);

                // Create new mappings
                var newMappings = new List<AccountingMapping>();

                if (!string.IsNullOrEmpty(mappingConfig.TaxPayableAccountId))
                    newMappings.Add(CreateAccountMapping(connection.Id, "TaxPayable", mappingConfig.TaxPayableAccountId));

                if (!string.IsNullOrEmpty(mappingConfig.TaxExpenseAccountId))
                    newMappings.Add(CreateAccountMapping(connection.Id, "TaxExpense", mappingConfig.TaxExpenseAccountId));

                if (!string.IsNullOrEmpty(mappingConfig.BankAccountId))
                    newMappings.Add(CreateAccountMapping(connection.Id, "Bank", mappingConfig.BankAccountId));

                if (!string.IsNullOrEmpty(mappingConfig.RevenueAccountId))
                    newMappings.Add(CreateAccountMapping(connection.Id, "Revenue", mappingConfig.RevenueAccountId));

                if (!string.IsNullOrEmpty(mappingConfig.PenaltyAccountId))
                    newMappings.Add(CreateAccountMapping(connection.Id, "Penalty", mappingConfig.PenaltyAccountId));

                if (!string.IsNullOrEmpty(mappingConfig.InterestAccountId))
                    newMappings.Add(CreateAccountMapping(connection.Id, "Interest", mappingConfig.InterestAccountId));

                // Add custom mappings
                foreach (var customMapping in mappingConfig.CustomMappings)
                {
                    newMappings.Add(CreateAccountMapping(connection.Id, customMapping.Key, customMapping.Value));
                }

                await _context.AccountingMappings.AddRangeAsync(newMappings);
                await _context.SaveChangesAsync();

                return new AccountingMappingResult
                {
                    IsSuccess = true,
                    Mapping = mappingConfig
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account mapping for client {ClientId}", clientId);
                return new AccountingMappingResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Gets synchronization history for the client
        /// </summary>
        public async Task<List<AccountingSyncHistoryDto>> GetSyncHistoryAsync(int clientId, int page = 1, int pageSize = 50)
        {
            try
            {
                var connection = await GetActiveConnectionAsync(clientId);
                if (connection == null) return new List<AccountingSyncHistoryDto>();

                var history = await _context.AccountingSyncHistory
                    .Where(h => h.AccountingConnectionId == connection.Id)
                    .OrderByDescending(h => h.SyncStartedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new AccountingSyncHistoryDto
                    {
                        Id = h.Id,
                        ClientId = clientId,
                        AccountingSystem = ProviderName,
                        SyncType = h.SyncType,
                        SyncTimestamp = h.SyncStartedAt,
                        IsSuccess = h.IsSuccess,
                        RecordsProcessed = h.RecordsProcessed,
                        ErrorMessage = h.ErrorMessage,
                        InitiatedBy = h.InitiatedBy
                    })
                    .ToListAsync();

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync history for client {ClientId}", clientId);
                return new List<AccountingSyncHistoryDto>();
            }
        }

        // Private helper methods

        private async Task<AccountingConnection?> GetActiveConnectionAsync(int clientId)
        {
            return await _context.AccountingConnections
                .FirstOrDefaultAsync(c => c.ClientId == clientId && 
                                         c.AccountingSystem == ProviderName && 
                                         c.IsActive);
        }

        private async Task<QuickBooksCompanyInfo?> GetCompanyInfoAsync(AccountingConnection connection)
        {
            try
            {
                var url = $"{_baseUrl}/v3/company/{connection.CompanyId}/companyinfo/{connection.CompanyId}";
                
                // Add OAuth header
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", DecryptToken(connection.EncryptedAccessToken));

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var qbResponse = JsonSerializer.Deserialize<QuickBooksCompanyInfoResponse>(content, _jsonOptions);
                    return qbResponse?.QueryResponse?.CompanyInfo?.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company info from QuickBooks");
            }
            
            return null;
        }

        private async Task SyncSinglePaymentAsync(AccountingConnection connection, AccountMappingDto mapping, Payment payment)
        {
            // Implementation for syncing a single payment as a journal entry
            // This would create a journal entry in QuickBooks with appropriate debit/credit entries
            
            var journalEntry = new
            {
                TxnDate = payment.PaymentDate.ToString("yyyy-MM-dd"),
                Line = new[]
                {
                    new
                    {
                        Amount = payment.Amount,
                        DetailType = "JournalEntryLineDetail",
                        JournalEntryLineDetail = new
                        {
                            PostingType = "Debit",
                            AccountRef = new { value = mapping.BankAccountId }
                        }
                    },
                    new
                    {
                        Amount = payment.Amount,
                        DetailType = "JournalEntryLineDetail", 
                        JournalEntryLineDetail = new
                        {
                            PostingType = "Credit",
                            AccountRef = new { value = mapping.TaxPayableAccountId }
                        }
                    }
                }
            };

            var url = $"{_baseUrl}/v3/company/{connection.CompanyId}/journalentry";
            
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", DecryptToken(connection.EncryptedAccessToken));

            var response = await _httpClient.PostAsJsonAsync(url, journalEntry, _jsonOptions);
            response.EnsureSuccessStatusCode();
        }

        private async Task SyncSingleTaxFilingAsync(AccountingConnection connection, AccountMappingDto mapping, TaxFiling taxFiling)
        {
            // Similar implementation for tax filings
            // This would create appropriate journal entries for tax liabilities
        }

        private async Task<List<AccountingTransactionDto>> GetTransactionsAsync(AccountingConnection connection, DateTime fromDate, DateTime toDate)
        {
            // Implementation to fetch transactions from QuickBooks
            return new List<AccountingTransactionDto>();
        }

        private async Task<List<AccountingCustomerDto>> GetCustomersAsync(AccountingConnection connection)
        {
            // Implementation to fetch customers from QuickBooks
            return new List<AccountingCustomerDto>();
        }

        private async Task<List<AccountingAccountDto>> GetAccountsAsync(AccountingConnection connection)
        {
            // Implementation to fetch chart of accounts from QuickBooks
            return new List<AccountingAccountDto>();
        }

        private AccountingMapping CreateAccountMapping(int connectionId, string accountType, string externalAccountId)
        {
            return new AccountingMapping
            {
                AccountingConnectionId = connectionId,
                AccountType = accountType,
                ExternalAccountId = externalAccountId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private async Task RecordSyncHistoryAsync(int connectionId, string syncType, string direction, AccountingSyncResult result)
        {
            var history = new AccountingSyncHistory
            {
                AccountingConnectionId = connectionId,
                SyncType = syncType,
                Direction = direction,
                SyncStartedAt = result.SyncTimestamp,
                SyncCompletedAt = DateTime.UtcNow,
                IsSuccess = result.IsSuccess,
                RecordsProcessed = result.RecordsProcessed,
                RecordsSucceeded = result.RecordsSucceeded,
                RecordsFailed = result.RecordsFailed,
                ErrorMessage = result.ErrorMessage,
                ErrorDetailsJson = result.Errors.Any() ? JsonSerializer.Serialize(result.Errors, _jsonOptions) : null,
                SyncDetailsJson = JsonSerializer.Serialize(result.Metadata, _jsonOptions),
                InitiatedBy = "System"
            };

            await _context.AccountingSyncHistory.AddAsync(history);
            await _context.SaveChangesAsync();
        }

        private string DecryptToken(string? encryptedToken)
        {
            // TODO: Implement proper token decryption
            // For now, return as-is (tokens should be encrypted in production)
            return encryptedToken ?? string.Empty;
        }
    }

    // QuickBooks API response models
    public class QuickBooksTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string RealmId { get; set; } = string.Empty;
    }

    public class QuickBooksCompanyInfoResponse
    {
        public QuickBooksQueryResponse QueryResponse { get; set; } = new();
    }

    public class QuickBooksQueryResponse
    {
        public List<QuickBooksCompanyInfo> CompanyInfo { get; set; } = new();
    }

    public class QuickBooksCompanyInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
    }
}