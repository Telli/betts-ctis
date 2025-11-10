using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BettsTax.Web.Controllers
{
    /// <summary>
    /// API controller for managing accounting system integrations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountingIntegrationsController : ControllerBase
    {
        private readonly IAccountingIntegrationFactory _integrationFactory;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountingIntegrationsController> _logger;

        public AccountingIntegrationsController(
            IAccountingIntegrationFactory integrationFactory,
            ApplicationDbContext context,
            ILogger<AccountingIntegrationsController> logger)
        {
            _integrationFactory = integrationFactory;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all available accounting integration providers
        /// </summary>
        [HttpGet("providers")]
        public ActionResult<List<AccountingProviderInfo>> GetAvailableProviders()
        {
            try
            {
                var providers = _integrationFactory.GetAvailableProviders();
                return Ok(providers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available accounting providers");
                return StatusCode(500, "Failed to retrieve accounting providers");
            }
        }

        /// <summary>
        /// Gets accounting connections for a specific client
        /// </summary>
        [HttpGet("client/{clientId}/connections")]
        public async Task<ActionResult<List<AccountingConnectionDto>>> GetClientConnections(int clientId)
        {
            try
            {
                var connections = await _context.AccountingConnections
                    .Where(c => c.ClientId == clientId)
                    .Select(c => new AccountingConnectionDto
                    {
                        Id = c.Id,
                        ClientId = c.ClientId,
                        AccountingSystem = c.AccountingSystem,
                        CompanyId = c.CompanyId,
                        CompanyName = c.CompanyName,
                        IsActive = c.IsActive,
                        ConnectedAt = c.ConnectedAt,
                        LastSyncAt = c.LastSyncAt
                    })
                    .ToListAsync();

                return Ok(connections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting connections for client {ClientId}", clientId);
                return StatusCode(500, "Failed to retrieve client connections");
            }
        }

        /// <summary>
        /// Initiates OAuth authentication for an accounting system
        /// </summary>
        [HttpPost("client/{clientId}/auth/{provider}/start")]
        public async Task<ActionResult<AccountingAuthResult>> StartAuthentication(
            int clientId, 
            string provider,
            [FromBody] AuthStartRequest request)
        {
            try
            {
                if (!_integrationFactory.IsProviderSupported(provider))
                {
                    return BadRequest($"Provider '{provider}' is not supported");
                }

                var integrationService = _integrationFactory.GetIntegrationService(provider);
                var result = await integrationService.AuthenticateAsync(
                    clientId.ToString(), 
                    request.RedirectUri, 
                    request.State);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting authentication for client {ClientId} with provider {Provider}", 
                    clientId, provider);
                return StatusCode(500, "Failed to start authentication");
            }
        }

        /// <summary>
        /// Completes OAuth authentication after callback
        /// </summary>
        [HttpPost("client/{clientId}/auth/{provider}/complete")]
        public async Task<ActionResult<AccountingAuthResult>> CompleteAuthentication(
            int clientId,
            string provider,
            [FromBody] AuthCompleteRequest request)
        {
            try
            {
                if (!_integrationFactory.IsProviderSupported(provider))
                {
                    return BadRequest($"Provider '{provider}' is not supported");
                }

                var integrationService = _integrationFactory.GetIntegrationService(provider);
                var result = await integrationService.CompleteAuthenticationAsync(
                    request.AuthCode,
                    request.State,
                    request.RedirectUri);

                if (result.IsSuccess)
                {
                    // Store the connection in database
                    await StoreConnectionAsync(clientId, provider, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing authentication for client {ClientId} with provider {Provider}", 
                    clientId, provider);
                return StatusCode(500, "Failed to complete authentication");
            }
        }

        /// <summary>
        /// Tests connection to an accounting system
        /// </summary>
        [HttpPost("client/{clientId}/connection/{provider}/test")]
        public async Task<ActionResult<AccountingConnectionResult>> TestConnection(int clientId, string provider)
        {
            try
            {
                if (!_integrationFactory.IsProviderSupported(provider))
                {
                    return BadRequest($"Provider '{provider}' is not supported");
                }

                var integrationService = _integrationFactory.GetIntegrationService(provider);
                var result = await integrationService.TestConnectionAsync(clientId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection for client {ClientId} with provider {Provider}", 
                    clientId, provider);
                return StatusCode(500, "Failed to test connection");
            }
        }

        /// <summary>
        /// Syncs payments to accounting system
        /// </summary>
        [HttpPost("client/{clientId}/sync/{provider}/payments")]
        public async Task<ActionResult<AccountingSyncResult>> SyncPayments(
            int clientId, 
            string provider,
            [FromBody] SyncPaymentsRequest request)
        {
            try
            {
                if (!_integrationFactory.IsProviderSupported(provider))
                {
                    return BadRequest($"Provider '{provider}' is not supported");
                }

                // Get payments to sync
                var paymentsQuery = _context.Payments.Where(p => p.ClientId == clientId);

                if (request.PaymentIds?.Any() == true)
                {
                    paymentsQuery = paymentsQuery.Where(p => request.PaymentIds.Contains(p.Id));
                }
                else if (request.FromDate.HasValue || request.ToDate.HasValue)
                {
                    if (request.FromDate.HasValue)
                        paymentsQuery = paymentsQuery.Where(p => p.PaymentDate >= request.FromDate.Value);
                    
                    if (request.ToDate.HasValue)
                        paymentsQuery = paymentsQuery.Where(p => p.PaymentDate <= request.ToDate.Value);
                }

                var payments = await paymentsQuery.ToListAsync();

                if (!payments.Any())
                {
                    return BadRequest("No payments found to sync");
                }

                var integrationService = _integrationFactory.GetIntegrationService(provider);
                var result = await integrationService.SyncPaymentsAsync(clientId, payments);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing payments for client {ClientId} with provider {Provider}", 
                    clientId, provider);
                return StatusCode(500, "Failed to sync payments");
            }
        }

        /// <summary>
        /// Syncs tax filings to accounting system
        /// </summary>
        [HttpPost("client/{clientId}/sync/{provider}/tax-filings")]
        public async Task<ActionResult<AccountingSyncResult>> SyncTaxFilings(
            int clientId,
            string provider,
            [FromBody] SyncTaxFilingsRequest request)
        {
            try
            {
                if (!_integrationFactory.IsProviderSupported(provider))
                {
                    return BadRequest($"Provider '{provider}' is not supported");
                }

                // Get tax filings to sync
                var taxFilingsQuery = _context.TaxFilings.Where(tf => tf.ClientId == clientId);

                if (request.TaxFilingIds?.Any() == true)
                {
                    taxFilingsQuery = taxFilingsQuery.Where(tf => request.TaxFilingIds.Contains(tf.Id));
                }
                else if (request.FromDate.HasValue || request.ToDate.HasValue)
                {
                    if (request.FromDate.HasValue)
                        taxFilingsQuery = taxFilingsQuery.Where(tf => tf.CreatedDate >= request.FromDate.Value);
                    
                    if (request.ToDate.HasValue)
                        taxFilingsQuery = taxFilingsQuery.Where(tf => tf.CreatedDate <= request.ToDate.Value);
                }

                var taxFilings = await taxFilingsQuery.ToListAsync();

                if (!taxFilings.Any())
                {
                    return BadRequest("No tax filings found to sync");
                }

                var integrationService = _integrationFactory.GetIntegrationService(provider);
                var result = await integrationService.SyncTaxFilingsAsync(clientId, taxFilings);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing tax filings for client {ClientId} with provider {Provider}", 
                    clientId, provider);
                return StatusCode(500, "Failed to sync tax filings");
            }
        }

        /// <summary>
        /// Imports financial data from accounting system
        /// </summary>
        [HttpPost("client/{clientId}/import/{provider}")]
        public async Task<ActionResult<AccountingImportResult>> ImportFinancialData(
            int clientId,
            string provider,
            [FromBody] ImportDataRequest request)
        {
            try
            {
                if (!_integrationFactory.IsProviderSupported(provider))
                {
                    return BadRequest($"Provider '{provider}' is not supported");
                }

                var integrationService = _integrationFactory.GetIntegrationService(provider);
                var result = await integrationService.ImportFinancialDataAsync(
                    clientId, 
                    request.FromDate, 
                    request.ToDate);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing data for client {ClientId} from provider {Provider}", 
                    clientId, provider);
                return StatusCode(500, "Failed to import financial data");
            }
        }

        /// <summary>
        /// Gets account mapping configuration for a provider
        /// </summary>
        [HttpGet("client/{clientId}/mapping/{provider}")]
        public async Task<ActionResult<AccountingMappingResult>> GetAccountMapping(int clientId, string provider)
        {
            try
            {
                if (!_integrationFactory.IsProviderSupported(provider))
                {
                    return BadRequest($"Provider '{provider}' is not supported");
                }

                var integrationService = _integrationFactory.GetIntegrationService(provider);
                var result = await integrationService.GetAccountMappingAsync(clientId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account mapping for client {ClientId} with provider {Provider}", 
                    clientId, provider);
                return StatusCode(500, "Failed to get account mapping");
            }
        }

        /// <summary>
        /// Updates account mapping configuration for a provider
        /// </summary>
        [HttpPut("client/{clientId}/mapping/{provider}")]
        public async Task<ActionResult<AccountingMappingResult>> UpdateAccountMapping(
            int clientId,
            string provider,
            [FromBody] AccountMappingDto mappingConfig)
        {
            try
            {
                if (!_integrationFactory.IsProviderSupported(provider))
                {
                    return BadRequest($"Provider '{provider}' is not supported");
                }

                mappingConfig.ClientId = clientId;
                mappingConfig.AccountingSystemName = provider;

                var integrationService = _integrationFactory.GetIntegrationService(provider);
                var result = await integrationService.UpdateAccountMappingAsync(clientId, mappingConfig);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account mapping for client {ClientId} with provider {Provider}", 
                    clientId, provider);
                return StatusCode(500, "Failed to update account mapping");
            }
        }

        /// <summary>
        /// Gets synchronization history for a client and provider
        /// </summary>
        [HttpGet("client/{clientId}/sync-history/{provider}")]
        public async Task<ActionResult<List<AccountingSyncHistoryDto>>> GetSyncHistory(
            int clientId, 
            string provider,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (!_integrationFactory.IsProviderSupported(provider))
                {
                    return BadRequest($"Provider '{provider}' is not supported");
                }

                var integrationService = _integrationFactory.GetIntegrationService(provider);
                var result = await integrationService.GetSyncHistoryAsync(clientId, page, pageSize);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync history for client {ClientId} with provider {Provider}", 
                    clientId, provider);
                return StatusCode(500, "Failed to get sync history");
            }
        }

        /// <summary>
        /// Disconnects an accounting system integration
        /// </summary>
        [HttpDelete("client/{clientId}/connection/{provider}")]
        public async Task<ActionResult> DisconnectProvider(int clientId, string provider)
        {
            try
            {
                var connection = await _context.AccountingConnections
                    .FirstOrDefaultAsync(c => c.ClientId == clientId && 
                                             c.AccountingSystem == provider && 
                                             c.IsActive);

                if (connection == null)
                {
                    return NotFound("Connection not found");
                }

                connection.IsActive = false;
                connection.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Provider disconnected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting provider {Provider} for client {ClientId}", 
                    provider, clientId);
                return StatusCode(500, "Failed to disconnect provider");
            }
        }

        // Private helper methods

        private async Task StoreConnectionAsync(int clientId, string provider, AccountingAuthResult authResult)
        {
            try
            {
                // Check for existing connection
                var existingConnection = await _context.AccountingConnections
                    .FirstOrDefaultAsync(c => c.ClientId == clientId && 
                                             c.AccountingSystem == provider);

                if (existingConnection != null)
                {
                    // Update existing connection
                    existingConnection.CompanyId = authResult.CompanyId;
                    existingConnection.CompanyName = authResult.CompanyName;
                    existingConnection.EncryptedAccessToken = EncryptToken(authResult.AccessToken);
                    existingConnection.EncryptedRefreshToken = EncryptToken(authResult.RefreshToken);
                    existingConnection.TokenExpiresAt = authResult.ExpiresAt;
                    existingConnection.IsActive = true;
                    existingConnection.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new connection
                    var newConnection = new AccountingConnection
                    {
                        ClientId = clientId,
                        AccountingSystem = provider,
                        CompanyId = authResult.CompanyId,
                        CompanyName = authResult.CompanyName,
                        EncryptedAccessToken = EncryptToken(authResult.AccessToken),
                        EncryptedRefreshToken = EncryptToken(authResult.RefreshToken),
                        TokenExpiresAt = authResult.ExpiresAt,
                        IsActive = true,
                        ConnectedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _context.AccountingConnections.AddAsync(newConnection);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing connection for client {ClientId} with provider {Provider}", 
                    clientId, provider);
                throw;
            }
        }

        private string? EncryptToken(string? token)
        {
            // TODO: Implement proper token encryption
            // For now, return as-is (tokens should be encrypted in production)
            return token;
        }
    }

    // Request/Response models

    public class AuthStartRequest
    {
        public string RedirectUri { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    public class AuthCompleteRequest
    {
        public string AuthCode { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
    }

    public class SyncPaymentsRequest
    {
        public List<int>? PaymentIds { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class SyncTaxFilingsRequest
    {
        public List<int>? TaxFilingIds { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class ImportDataRequest
    {
        public DateTime FromDate { get; set; } = DateTime.Now.AddMonths(-1);
        public DateTime ToDate { get; set; } = DateTime.Now;
    }
}