using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace BettsTax.Core.Jobs;

[DisallowConcurrentExecution]
public class ReportSchedulingJob : IJob
{
    private readonly ILogger<ReportSchedulingJob> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IReportService _reportService;

    public ReportSchedulingJob(
        ILogger<ReportSchedulingJob> logger,
        ApplicationDbContext context,
        IReportService reportService)
    {
        _logger = logger;
        _context = context;
        _reportService = reportService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting report scheduling job at {Timestamp}", DateTime.UtcNow);

        try
        {
            // Process pending scheduled reports
            var pendingReports = await _context.ReportRequests
                .Where(r => r.Status == ReportStatus.Pending)
                .OrderBy(r => r.RequestedAt)
                .Take(10) // Process up to 10 reports per run
                .ToListAsync();

            foreach (var reportRequest in pendingReports)
            {
                try
                {
                    _logger.LogInformation("Processing scheduled report {RequestId} of type {Type}", 
                        reportRequest.RequestId, reportRequest.Type);

                    // Mark as processing
                    reportRequest.Status = ReportStatus.Processing;
                    await _context.SaveChangesAsync();

                    // Generate the report based on type
                    byte[] reportData = await GenerateReportByType(reportRequest);

                    // Update status to completed
                    reportRequest.Status = ReportStatus.Completed;
                    reportRequest.CompletedAt = DateTime.UtcNow;
                    reportRequest.DownloadUrl = await SaveReportFile(reportRequest, reportData);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully processed scheduled report {RequestId}", 
                        reportRequest.RequestId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing scheduled report {RequestId}", 
                        reportRequest.RequestId);

                    // Mark as failed
                    reportRequest.Status = ReportStatus.Failed;
                    reportRequest.ErrorMessage = ex.Message;
                    reportRequest.CompletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            // Clean up old completed/failed reports (older than 30 days)
            await CleanupOldReports();

            _logger.LogInformation("Report scheduling job completed at {Timestamp}. Processed {Count} reports.", 
                DateTime.UtcNow, pendingReports.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing report scheduling job");
            throw;
        }
    }

    private async Task<byte[]> GenerateReportByType(ReportRequest reportRequest)
    {
        var parameters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
            reportRequest.Parameters ?? "{}") ?? new Dictionary<string, object>();

        return reportRequest.Type switch
        {
            ReportType.TaxFiling => await _reportService.GenerateTaxFilingReportAsync(
                GetIntParameter(parameters, "clientId"),
                GetIntParameter(parameters, "taxYear"),
                reportRequest.Format),

            ReportType.PaymentHistory => await _reportService.GeneratePaymentHistoryReportAsync(
                GetIntParameter(parameters, "clientId"),
                GetDateParameter(parameters, "fromDate"),
                GetDateParameter(parameters, "toDate"),
                reportRequest.Format),

            ReportType.Compliance => await _reportService.GenerateComplianceReportAsync(
                GetIntParameter(parameters, "clientId"),
                GetDateParameter(parameters, "fromDate"),
                GetDateParameter(parameters, "toDate"),
                reportRequest.Format),

            ReportType.ClientActivity => await _reportService.GenerateClientActivityReportAsync(
                GetDateParameter(parameters, "fromDate"),
                GetDateParameter(parameters, "toDate"),
                reportRequest.Format),

            ReportType.DocumentSubmission => await _reportService.GenerateDocumentSubmissionReportAsync(
                GetIntParameter(parameters, "clientId"),
                GetDateParameter(parameters, "fromDate"),
                GetDateParameter(parameters, "toDate"),
                reportRequest.Format),

            ReportType.TaxCalendar => await _reportService.GenerateTaxCalendarReportAsync(
                GetOptionalIntParameter(parameters, "clientId"),
                GetIntParameter(parameters, "taxYear"),
                reportRequest.Format),

            ReportType.ClientComplianceOverview => await _reportService.GenerateClientComplianceOverviewReportAsync(
                GetDateParameter(parameters, "fromDate"),
                GetDateParameter(parameters, "toDate"),
                reportRequest.Format),

            ReportType.Revenue => await _reportService.GenerateRevenueReportAsync(
                GetDateParameter(parameters, "fromDate"),
                GetDateParameter(parameters, "toDate"),
                reportRequest.Format),

            ReportType.CaseManagement => await _reportService.GenerateCaseManagementReportAsync(
                GetDateParameter(parameters, "fromDate"),
                GetDateParameter(parameters, "toDate"),
                reportRequest.Format),

            ReportType.EnhancedClientActivity => await _reportService.GenerateEnhancedClientActivityReportAsync(
                GetDateParameter(parameters, "fromDate"),
                GetDateParameter(parameters, "toDate"),
                GetOptionalStringParameter(parameters, "clientFilter"),
                GetOptionalStringParameter(parameters, "activityTypeFilter"),
                reportRequest.Format),

            _ => throw new ArgumentException($"Unsupported report type: {reportRequest.Type}")
        };
    }

    private async Task<string> SaveReportFile(ReportRequest reportRequest, byte[] reportData)
    {
        var reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Reports", "Generated");
        Directory.CreateDirectory(reportsDirectory);

        var fileExtension = reportRequest.Format switch
        {
            ReportFormat.PDF => "pdf",
            ReportFormat.Excel => "xlsx",
            ReportFormat.CSV => "csv",
            _ => "bin"
        };

        var fileName = $"{reportRequest.Type}_{reportRequest.RequestId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fileExtension}";
        var filePath = Path.Combine(reportsDirectory, fileName);

        await File.WriteAllBytesAsync(filePath, reportData);
        return filePath;
    }

    private async Task CleanupOldReports()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        
        var oldReports = await _context.ReportRequests
            .Where(r => (r.Status == ReportStatus.Completed || r.Status == ReportStatus.Failed) &&
                       r.CompletedAt.HasValue && r.CompletedAt < cutoffDate)
            .ToListAsync();

        foreach (var report in oldReports)
        {
            // Delete physical file if it exists
            if (!string.IsNullOrEmpty(report.DownloadUrl) && File.Exists(report.DownloadUrl))
            {
                try
                {
                    File.Delete(report.DownloadUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete report file {FilePath}", report.DownloadUrl);
                }
            }

            // Remove from database
            _context.ReportRequests.Remove(report);
        }

        if (oldReports.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} old reports", oldReports.Count);
        }
    }

    private static int GetIntParameter(Dictionary<string, object> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value))
        {
            if (value is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.Number)
                return element.GetInt32();
            if (int.TryParse(value?.ToString(), out var intValue))
                return intValue;
        }
        throw new ArgumentException($"Missing or invalid parameter: {key}");
    }

    private static int? GetOptionalIntParameter(Dictionary<string, object> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value))
        {
            if (value is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.Number)
                return element.GetInt32();
            if (int.TryParse(value?.ToString(), out var intValue))
                return intValue;
        }
        return null;
    }

    private static string? GetOptionalStringParameter(Dictionary<string, object> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value))
        {
            if (value is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.String)
                return element.GetString();
            return value?.ToString();
        }
        return null;
    }

    private static DateTime GetDateParameter(Dictionary<string, object> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value))
        {
            if (value is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                if (DateTime.TryParse(element.GetString(), out var dateValue))
                    return dateValue;
            }
            if (DateTime.TryParse(value?.ToString(), out var parsedDate))
                return parsedDate;
        }
        throw new ArgumentException($"Missing or invalid date parameter: {key}");
    }
}
