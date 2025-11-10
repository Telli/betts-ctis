using BettsTax.Core.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BettsTax.Core.Services;

public class ReportRateLimitService : IReportRateLimitService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<ReportRateLimitService> _logger;

    // Rate limit configurations
    private readonly Dictionary<string, RateLimitConfig> _rateLimits = new()
    {
        { "TaxFiling", new RateLimitConfig { MaxRequests = 10, WindowMinutes = 60 } },
        { "PaymentHistory", new RateLimitConfig { MaxRequests = 15, WindowMinutes = 60 } },
        { "Compliance", new RateLimitConfig { MaxRequests = 5, WindowMinutes = 60 } },
        { "ClientActivity", new RateLimitConfig { MaxRequests = 20, WindowMinutes = 60 } },
        { "DocumentSubmission", new RateLimitConfig { MaxRequests = 10, WindowMinutes = 60 } },
        { "TaxCalendar", new RateLimitConfig { MaxRequests = 5, WindowMinutes = 60 } },
        { "ClientComplianceOverview", new RateLimitConfig { MaxRequests = 3, WindowMinutes = 60 } },
        { "Revenue", new RateLimitConfig { MaxRequests = 5, WindowMinutes = 60 } },
        { "CaseManagement", new RateLimitConfig { MaxRequests = 10, WindowMinutes = 60 } },
        { "EnhancedClientActivity", new RateLimitConfig { MaxRequests = 15, WindowMinutes = 60 } }
    };

    public ReportRateLimitService(
        IDistributedCache cache,
        ILogger<ReportRateLimitService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> CanGenerateReportAsync(string userId, string reportType)
    {
        if (!_rateLimits.TryGetValue(reportType, out var config))
        {
            // No rate limit configured for this report type
            return true;
        }

        var cacheKey = GetCacheKey(userId, reportType);
        var rateLimitData = await GetRateLimitDataAsync(cacheKey);

        if (rateLimitData == null)
        {
            // No previous requests recorded
            return true;
        }

        // Clean up expired entries
        var cutoffTime = DateTime.UtcNow.AddMinutes(-config.WindowMinutes);
        rateLimitData.Requests = rateLimitData.Requests
            .Where(r => r > cutoffTime)
            .ToList();

        // Check if under the limit
        var canGenerate = rateLimitData.Requests.Count < config.MaxRequests;

        if (!canGenerate)
        {
            _logger.LogWarning("Rate limit exceeded for user {UserId} and report type {ReportType}. " +
                             "Current requests: {CurrentRequests}, Max allowed: {MaxRequests}",
                userId, reportType, rateLimitData.Requests.Count, config.MaxRequests);
        }

        return canGenerate;
    }

    public async Task RecordReportGenerationAsync(string userId, string reportType)
    {
        if (!_rateLimits.TryGetValue(reportType, out var config))
        {
            // No rate limit configured for this report type
            return;
        }

        var cacheKey = GetCacheKey(userId, reportType);
        var rateLimitData = await GetRateLimitDataAsync(cacheKey) ?? new RateLimitData();

        // Clean up expired entries
        var cutoffTime = DateTime.UtcNow.AddMinutes(-config.WindowMinutes);
        rateLimitData.Requests = rateLimitData.Requests
            .Where(r => r > cutoffTime)
            .ToList();

        // Add current request
        rateLimitData.Requests.Add(DateTime.UtcNow);

        // Save back to cache
        await SetRateLimitDataAsync(cacheKey, rateLimitData, config.WindowMinutes);

        _logger.LogDebug("Recorded report generation for user {UserId} and report type {ReportType}. " +
                        "Total requests in window: {RequestCount}",
            userId, reportType, rateLimitData.Requests.Count);
    }

    public async Task<int> GetRemainingQuotaAsync(string userId, string reportType)
    {
        if (!_rateLimits.TryGetValue(reportType, out var config))
        {
            // No rate limit configured for this report type
            return int.MaxValue;
        }

        var cacheKey = GetCacheKey(userId, reportType);
        var rateLimitData = await GetRateLimitDataAsync(cacheKey);

        if (rateLimitData == null)
        {
            return config.MaxRequests;
        }

        // Clean up expired entries
        var cutoffTime = DateTime.UtcNow.AddMinutes(-config.WindowMinutes);
        var activeRequests = rateLimitData.Requests
            .Where(r => r > cutoffTime)
            .Count();

        return Math.Max(0, config.MaxRequests - activeRequests);
    }

    public async Task<TimeSpan?> GetTimeUntilResetAsync(string userId, string reportType)
    {
        if (!_rateLimits.TryGetValue(reportType, out var config))
        {
            // No rate limit configured for this report type
            return null;
        }

        var cacheKey = GetCacheKey(userId, reportType);
        var rateLimitData = await GetRateLimitDataAsync(cacheKey);

        if (rateLimitData == null || !rateLimitData.Requests.Any())
        {
            return null;
        }

        // Find the oldest request that's still within the window
        var cutoffTime = DateTime.UtcNow.AddMinutes(-config.WindowMinutes);
        var oldestActiveRequest = rateLimitData.Requests
            .Where(r => r > cutoffTime)
            .OrderBy(r => r)
            .FirstOrDefault();

        if (oldestActiveRequest == default)
        {
            return null;
        }

        // Calculate when the oldest request will expire
        var resetTime = oldestActiveRequest.AddMinutes(config.WindowMinutes);
        var timeUntilReset = resetTime - DateTime.UtcNow;

        return timeUntilReset > TimeSpan.Zero ? timeUntilReset : null;
    }

    public async Task CleanupExpiredEntriesAsync()
    {
        // This method would ideally iterate through all cache keys, but since we're using
        // IDistributedCache, we don't have a way to enumerate keys. In a production system,
        // you might want to use a different caching strategy or maintain a separate index
        // of active rate limit keys.

        _logger.LogInformation("Rate limit cleanup completed. Note: Cleanup is handled per-request basis.");
    }

    private static string GetCacheKey(string userId, string reportType)
    {
        return $"report_rate_limit:{userId}:{reportType}";
    }

    private async Task<RateLimitData?> GetRateLimitDataAsync(string cacheKey)
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (string.IsNullOrEmpty(cachedData))
            {
                return null;
            }

            return JsonSerializer.Deserialize<RateLimitData>(cachedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rate limit data for key {CacheKey}", cacheKey);
            return null;
        }
    }

    private async Task SetRateLimitDataAsync(string cacheKey, RateLimitData data, int windowMinutes)
    {
        try
        {
            var serializedData = JsonSerializer.Serialize(data);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(windowMinutes + 5) // Add buffer
            };

            await _cache.SetStringAsync(cacheKey, serializedData, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting rate limit data for key {CacheKey}", cacheKey);
        }
    }

    private class RateLimitData
    {
        public List<DateTime> Requests { get; set; } = new();
    }

    private class RateLimitConfig
    {
        public int MaxRequests { get; set; }
        public int WindowMinutes { get; set; }
    }
}
