namespace BettsTax.Core.Services.Interfaces;

public interface IReportRateLimitService
{
    /// <summary>
    /// Checks if a user can generate a report based on rate limiting rules
    /// </summary>
    /// <param name="userId">User ID requesting the report</param>
    /// <param name="reportType">Type of report being requested</param>
    /// <returns>True if allowed, false if rate limited</returns>
    Task<bool> CanGenerateReportAsync(string userId, string reportType);

    /// <summary>
    /// Records a report generation attempt for rate limiting tracking
    /// </summary>
    /// <param name="userId">User ID generating the report</param>
    /// <param name="reportType">Type of report being generated</param>
    Task RecordReportGenerationAsync(string userId, string reportType);

    /// <summary>
    /// Gets the remaining quota for a user and report type
    /// </summary>
    /// <param name="userId">User ID to check</param>
    /// <param name="reportType">Type of report to check</param>
    /// <returns>Number of reports remaining in current time window</returns>
    Task<int> GetRemainingQuotaAsync(string userId, string reportType);

    /// <summary>
    /// Gets the time until the rate limit resets for a user and report type
    /// </summary>
    /// <param name="userId">User ID to check</param>
    /// <param name="reportType">Type of report to check</param>
    /// <returns>Time until reset, or null if no limit is active</returns>
    Task<TimeSpan?> GetTimeUntilResetAsync(string userId, string reportType);

    /// <summary>
    /// Cleans up expired rate limit entries
    /// </summary>
    Task CleanupExpiredEntriesAsync();
}
