using BettsTax.Data;
using BettsTax.Web.Options;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace BettsTax.Web.Services;

public interface ITaxAuthorityService
{
    Task<TaxAuthoritySubmissionResponse> SubmitTaxFilingAsync(int taxFilingId);
    Task<TaxAuthorityStatusResponse> CheckFilingStatusAsync(string authorityReference);
    Task<bool> ValidateConfigurationAsync();
    Task ProcessPendingSubmissionsAsync();
    Task ProcessStatusChecksAsync();
}

public class TaxAuthorityService : ITaxAuthorityService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOptions<TaxAuthorityOptions> _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TaxAuthorityService> _logger;

    public TaxAuthorityService(
        ApplicationDbContext dbContext,
        IOptions<TaxAuthorityOptions> options,
        HttpClient httpClient,
        ILogger<TaxAuthorityService> logger)
    {
        _dbContext = dbContext;
        _options = options;
        _httpClient = httpClient;
        _logger = logger;

        // Configure HTTP client safely (do not throw when BaseUrl is empty/invalid)
        if (!string.IsNullOrWhiteSpace(_options.Value.BaseUrl))
        {
            try
            {
                _httpClient.BaseAddress = new Uri(_options.Value.BaseUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid TaxAuthority BaseUrl: {BaseUrl}. HTTP client BaseAddress not set.", _options.Value.BaseUrl);
            }
        }
        else
        {
            _logger.LogWarning("TaxAuthorityOptions.BaseUrl is empty; TaxAuthorityService HTTP client will not be configured (expected in local/dev).");
        }

        // Configure timeout and default headers
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.Value.TimeoutSeconds);

        if (!string.IsNullOrWhiteSpace(_options.Value.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _options.Value.ApiKey);
        }
        if (!string.IsNullOrEmpty(_options.Value.ClientId))
        {
            _httpClient.DefaultRequestHeaders.Remove("X-Client-ID");
            _httpClient.DefaultRequestHeaders.Add("X-Client-ID", _options.Value.ClientId);
        }
        if (_options.Value.Headers != null)
        {
            foreach (var header in _options.Value.Headers)
            {
                if (!string.IsNullOrEmpty(header.Key) && !string.IsNullOrEmpty(header.Value))
                {
                    _httpClient.DefaultRequestHeaders.Remove(header.Key);
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
        }
    }

    public async Task<TaxAuthoritySubmissionResponse> SubmitTaxFilingAsync(int taxFilingId)
    {
        try
        {
            // If not configured, return a non-fatal response to avoid blowing up unrelated endpoints
            if (_httpClient.BaseAddress == null || string.IsNullOrWhiteSpace(_options.Value.BaseUrl))
            {
                _logger.LogWarning("TaxAuthority BaseUrl not configured; skipping external submission.");
                return new TaxAuthoritySubmissionResponse
                {
                    Success = false,
                    Message = "Tax authority not configured"
                };
            }

            var taxFiling = await _dbContext.TaxFilings
                .Include(t => t.Client)
                .FirstOrDefaultAsync(t => t.TaxFilingId == taxFilingId);

            if (taxFiling == null)
            {
                throw new ArgumentException($"Tax filing with ID {taxFilingId} not found");
            }

            var request = new TaxAuthoritySubmissionRequest
            {
                TaxpayerTin = taxFiling.Client?.TIN ?? string.Empty,
                TaxType = taxFiling.TaxType.ToString(),
                TaxPeriod = taxFiling.FilingPeriod,
                TaxAmount = taxFiling.TaxAmount,
                PenaltyAmount = taxFiling.PenaltyAmount,
                InterestAmount = taxFiling.InterestAmount,
                DueDate = taxFiling.DueDate ?? DateTime.UtcNow,
                AdditionalData = taxFiling.AdditionalData
            };

            var endpoint = _options.Value.Endpoints?.GetValueOrDefault("submit", "/api/submit") ?? "/api/submit";

            var response = await _httpClient.PostAsJsonAsync(endpoint, request);
            var result = await response.Content.ReadFromJsonAsync<TaxAuthoritySubmissionResponse>();

            if (result?.Success == true && !string.IsNullOrEmpty(result.Reference))
            {
                var submission = new TaxAuthoritySubmission
                {
                    TaxFilingId = taxFilingId,
                    AuthorityReference = result.Reference,
                    SubmissionStatus = "Submitted",
                    SubmissionResponse = result.Message,
                    SubmittedAt = DateTime.UtcNow
                };

                _dbContext.TaxAuthoritySubmissions.Add(submission);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Tax filing {TaxFilingId} submitted to authority with reference {Reference}",
                    taxFilingId, result.Reference);
            }

            return result ?? new TaxAuthoritySubmissionResponse { Success = false, Message = "No response received" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting tax filing {TaxFilingId} to authority", taxFilingId);
            return new TaxAuthoritySubmissionResponse { Success = false, Message = ex.Message };
        }
    }

    public async Task<TaxAuthorityStatusResponse> CheckFilingStatusAsync(string authorityReference)
    {
        try
        {
            var request = new TaxAuthorityStatusRequest { Reference = authorityReference };
            var endpoint = _options.Value.Endpoints.GetValueOrDefault("status", "/api/status");

            var response = await _httpClient.PostAsJsonAsync(endpoint, request);
            var result = await response.Content.ReadFromJsonAsync<TaxAuthorityStatusResponse>();

            if (result?.Success == true)
            {
                // Update status check record
                var statusCheck = new TaxAuthorityStatusCheck
                {
                    TaxFilingId = 0, // Will be set by caller if needed
                    AuthorityReference = authorityReference,
                    Status = result.Status ?? "Unknown",
                    Details = result.Details,
                    CheckedAt = DateTime.UtcNow,
                    IsSuccessful = true
                };

                _dbContext.TaxAuthorityStatusChecks.Add(statusCheck);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Status check completed for reference {Reference}: {Status}",
                    authorityReference, result.Status);
            }

            return result ?? new TaxAuthorityStatusResponse { Success = false, Message = "No response received" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking status for reference {Reference}", authorityReference);
            return new TaxAuthorityStatusResponse { Success = false, Message = ex.Message };
        }
    }

    public async Task<bool> ValidateConfigurationAsync()
    {
        try
        {
            var options = _options.Value;

            if (string.IsNullOrEmpty(options.BaseUrl) ||
                string.IsNullOrEmpty(options.ApiKey))
            {
                _logger.LogWarning("Tax authority configuration is incomplete");
                return false;
            }

            // Test basic connectivity
            var testEndpoint = options.Endpoints.GetValueOrDefault("health", "/health");
            var response = await _httpClient.GetAsync(testEndpoint);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Tax authority configuration validation successful");
                return true;
            }
            else
            {
                _logger.LogWarning("Tax authority health check failed with status {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating tax authority configuration");
            return false;
        }
    }

    public async Task ProcessPendingSubmissionsAsync()
    {
        try
        {
            if (!_options.Value.EnableAutoSubmission)
            {
                _logger.LogInformation("Auto submission is disabled");
                return;
            }

            var pendingFilings = await _dbContext.TaxFilings
                .Where(t => t.Status == FilingStatus.Approved &&
                           !t.TaxAuthoritySubmissions.Any(s => s.SubmissionStatus == "Submitted"))
                .ToListAsync();

            foreach (var filing in pendingFilings)
            {
                await SubmitTaxFilingAsync(filing.TaxFilingId);
                await Task.Delay(1000); // Rate limiting
            }

            _logger.LogInformation("Processed {Count} pending tax filing submissions", pendingFilings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending submissions");
        }
    }

    public async Task ProcessStatusChecksAsync()
    {
        try
        {
            if (!_options.Value.EnableStatusChecks)
            {
                _logger.LogInformation("Status checks are disabled");
                return;
            }

            var submissionsToCheck = await _dbContext.TaxAuthoritySubmissions
                .Where(s => s.SubmissionStatus == "Submitted" &&
                           (!s.StatusLastChecked.HasValue ||
                            s.StatusLastChecked.Value.AddMinutes(_options.Value.StatusCheckIntervalMinutes) < DateTime.UtcNow))
                .ToListAsync();

            foreach (var submission in submissionsToCheck)
            {
                var statusResult = await CheckFilingStatusAsync(submission.AuthorityReference);

                if (statusResult.Success && !string.IsNullOrEmpty(statusResult.Status))
                {
                    submission.AuthorityStatus = statusResult.Status;
                    submission.StatusLastChecked = DateTime.UtcNow;

                    if (statusResult.Status == "Accepted" || statusResult.Status == "Rejected")
                    {
                        submission.ProcessedAt = DateTime.UtcNow;
                    }
                }

                await _dbContext.SaveChangesAsync();
                await Task.Delay(1000); // Rate limiting
            }

            _logger.LogInformation("Processed status checks for {Count} submissions", submissionsToCheck.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing status checks");
        }
    }
}