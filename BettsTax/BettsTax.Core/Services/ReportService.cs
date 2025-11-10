using BettsTax.Core.DTOs.Reports;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Text.Json;

namespace BettsTax.Core.Services;

public partial class ReportService : BettsTax.Core.Services.Interfaces.IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IReportTemplateService _templateService;
    private readonly IReportGenerator _reportGenerator;
    private readonly IScheduler _scheduler;
    private readonly IReportRateLimitService _rateLimitService;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        ApplicationDbContext context,
        IReportTemplateService templateService,
        IReportGenerator reportGenerator,
        IScheduler scheduler,
        IReportRateLimitService rateLimitService,
        ILogger<ReportService> logger)
    {
        _context = context;
        _templateService = templateService;
        _reportGenerator = reportGenerator;
        _scheduler = scheduler;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    public async Task<string> QueueReportGenerationAsync(CreateReportRequestDto request, string userId)
    {
        try
        {
            // Check rate limiting
            if (!await _rateLimitService.CanGenerateReportAsync(userId, request.Type.ToString()))
            {
                var remainingQuota = await _rateLimitService.GetRemainingQuotaAsync(userId, request.Type.ToString());
                var timeUntilReset = await _rateLimitService.GetTimeUntilResetAsync(userId, request.Type.ToString());

                throw new InvalidOperationException(
                    $"Rate limit exceeded for report type {request.Type}. " +
                    $"Remaining quota: {remainingQuota}. " +
                    $"Reset in: {timeUntilReset?.TotalMinutes:F0} minutes.");
            }

            var requestId = Guid.NewGuid().ToString();
            var user = await _context.Users.FindAsync(userId);

            var reportRequest = new ReportRequest
            {
                RequestId = requestId,
                Type = request.Type,
                Format = request.Format,
                Parameters = JsonSerializer.Serialize(request.Parameters),
                RequestedByUserId = userId,
                RequestedByUserName = user?.UserName ?? "Unknown User",
                Status = ReportStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7) // Reports expire after 7 days
            };

            _context.ReportRequests.Add(reportRequest);
            await _context.SaveChangesAsync();

            // Schedule report generation job using Quartz.NET
            var jobData = new JobDataMap
            {
                ["RequestId"] = requestId,
                ["UserId"] = userId
            };

            var job = JobBuilder.Create<ReportGenerationJob>()
                .WithIdentity($"report-{requestId}", "reports")
                .UsingJobData(jobData)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"report-trigger-{requestId}", "reports")
                .StartNow()
                .Build();

            await _scheduler.ScheduleJob(job, trigger);

            // Record the report generation for rate limiting
            await _rateLimitService.RecordReportGenerationAsync(userId, request.Type.ToString());

            _logger.LogInformation("Report generation queued with ID: {RequestId}", requestId);
            return requestId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing report generation for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ReportRequestDto?> GetReportStatusAsync(string requestId)
    {
        try
        {
            var reportRequest = await _context.ReportRequests
                .FirstOrDefaultAsync(rr => rr.RequestId == requestId);

            if (reportRequest == null)
                return null;

            return new ReportRequestDto
            {
                Id = reportRequest.Id,
                RequestId = reportRequest.RequestId,
                Type = reportRequest.Type,
                Format = reportRequest.Format,
                Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(reportRequest.Parameters) ?? new(),
                RequestedByUserId = reportRequest.RequestedByUserId,
                RequestedByUserName = reportRequest.RequestedByUserName,
                Status = reportRequest.Status,
                RequestedAt = reportRequest.RequestedAt,
                CompletedAt = reportRequest.CompletedAt,
                DownloadUrl = reportRequest.DownloadUrl,
                ErrorMessage = reportRequest.ErrorMessage,
                FileSizeBytes = reportRequest.FileSizeBytes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report status for request {RequestId}", requestId);
            throw;
        }
    }

    public async Task<List<ReportRequestDto>> GetUserReportsAsync(string userId, int pageSize = 20, int pageNumber = 1)
    {
        try
        {
            var reports = await _context.ReportRequests
                .Where(rr => rr.RequestedByUserId == userId)
                .OrderByDescending(rr => rr.RequestedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(rr => new ReportRequestDto
                {
                    Id = rr.Id,
                    RequestId = rr.RequestId,
                    Type = rr.Type,
                    Format = rr.Format,
                    Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(rr.Parameters, (JsonSerializerOptions?)null) ?? new(),
                    RequestedByUserId = rr.RequestedByUserId,
                    RequestedByUserName = rr.RequestedByUserName,
                    Status = rr.Status,
                    RequestedAt = rr.RequestedAt,
                    CompletedAt = rr.CompletedAt,
                    DownloadUrl = rr.DownloadUrl,
                    ErrorMessage = rr.ErrorMessage,
                    FileSizeBytes = rr.FileSizeBytes
                })
                .ToListAsync();

            return reports;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reports for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<ReportRequestDto>> GetUserReportsAsync(string userId, ReportHistoryFilter filter, int pageSize = 20, int pageNumber = 1)
    {
        try
        {
            var query = _context.ReportRequests.AsQueryable();

            query = query.Where(rr => rr.RequestedByUserId == userId);

            if (filter.Status.HasValue)
            {
                query = query.Where(rr => rr.Status == filter.Status.Value);
            }

            if (filter.Type.HasValue)
            {
                query = query.Where(rr => rr.Type == filter.Type.Value);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(rr => rr.RequestedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(rr => rr.RequestedAt <= filter.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(rr =>
                    (rr.RequestedByUserName != null && rr.RequestedByUserName.ToLower().Contains(term)) ||
                    (rr.ErrorMessage != null && rr.ErrorMessage.ToLower().Contains(term)) ||
                    (rr.DownloadUrl != null && rr.DownloadUrl.ToLower().Contains(term)) ||
                    (rr.FileName != null && rr.FileName.ToLower().Contains(term))
                );
            }

            // Sorting
            var sortBy = (filter.SortBy ?? "requestedAt").ToLower();
            var sortDir = (filter.SortDir ?? "desc").ToLower();
            var desc = sortDir == "desc";

            query = (sortBy) switch
            {
                "completedat" => desc ? query.OrderByDescending(rr => rr.CompletedAt) : query.OrderBy(rr => rr.CompletedAt),
                "status" => desc ? query.OrderByDescending(rr => rr.Status) : query.OrderBy(rr => rr.Status),
                "type" => desc ? query.OrderByDescending(rr => rr.Type) : query.OrderBy(rr => rr.Type),
                "filesize" => desc ? query.OrderByDescending(rr => rr.FileSizeBytes) : query.OrderBy(rr => rr.FileSizeBytes),
                _ => desc ? query.OrderByDescending(rr => rr.RequestedAt) : query.OrderBy(rr => rr.RequestedAt)
            };

            var reports = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(rr => new ReportRequestDto
                {
                    Id = rr.Id,
                    RequestId = rr.RequestId,
                    Type = rr.Type,
                    Format = rr.Format,
                    Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(rr.Parameters, (JsonSerializerOptions?)null) ?? new(),
                    RequestedByUserId = rr.RequestedByUserId,
                    RequestedByUserName = rr.RequestedByUserName,
                    Status = rr.Status,
                    RequestedAt = rr.RequestedAt,
                    CompletedAt = rr.CompletedAt,
                    DownloadUrl = rr.DownloadUrl,
                    ErrorMessage = rr.ErrorMessage,
                    FileSizeBytes = rr.FileSizeBytes
                })
                .ToListAsync();

            return reports;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filtered reports for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetUserReportCountAsync(string userId)
    {
        try
        {
            return await _context.ReportRequests
                .Where(rr => rr.RequestedByUserId == userId)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting reports for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetUserReportCountAsync(string userId, ReportHistoryFilter filter)
    {
        try
        {
            var query = _context.ReportRequests.AsQueryable();
            query = query.Where(rr => rr.RequestedByUserId == userId);

            if (filter.Status.HasValue)
            {
                query = query.Where(rr => rr.Status == filter.Status.Value);
            }

            if (filter.Type.HasValue)
            {
                query = query.Where(rr => rr.Type == filter.Type.Value);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(rr => rr.RequestedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(rr => rr.RequestedAt <= filter.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(rr =>
                    (rr.RequestedByUserName != null && rr.RequestedByUserName.ToLower().Contains(term)) ||
                    (rr.ErrorMessage != null && rr.ErrorMessage.ToLower().Contains(term)) ||
                    (rr.DownloadUrl != null && rr.DownloadUrl.ToLower().Contains(term)) ||
                    (rr.FileName != null && rr.FileName.ToLower().Contains(term))
                );
            }

            return await query.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting filtered reports for user {UserId}", userId);
            throw;
        }
    }

    public async Task<byte[]?> GetReportFileAsync(string requestId, string userId)
    {
        try
        {
            var reportRequest = await _context.ReportRequests
                .FirstOrDefaultAsync(rr => rr.RequestId == requestId && rr.RequestedByUserId == userId);

            if (reportRequest?.Status != ReportStatus.Completed || string.IsNullOrEmpty(reportRequest.DownloadUrl))
                return null;

            // Check if report has expired
            if (reportRequest.ExpiresAt.HasValue && reportRequest.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Report {RequestId} has expired", requestId);
                return null;
            }

            // Read file from storage
            var filePath = reportRequest.DownloadUrl;
            if (File.Exists(filePath))
            {
                return await File.ReadAllBytesAsync(filePath);
            }

            _logger.LogWarning("Report file not found for request {RequestId}", requestId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving report file for request {RequestId}", requestId);
            throw;
        }
    }

    public async Task<bool> DeleteReportAsync(string requestId, string userId)
    {
        try
        {
            var reportRequest = await _context.ReportRequests
                .FirstOrDefaultAsync(rr => rr.RequestId == requestId && rr.RequestedByUserId == userId);

            if (reportRequest == null)
                return false;

            // Delete physical file if it exists
            if (!string.IsNullOrEmpty(reportRequest.DownloadUrl) && File.Exists(reportRequest.DownloadUrl))
            {
                File.Delete(reportRequest.DownloadUrl);
            }

            _context.ReportRequests.Remove(reportRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted report {RequestId} for user {UserId}", requestId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report {RequestId} for user {UserId}", requestId, userId);
            throw;
        }
    }

    // Direct generation methods for smaller reports
    public async Task<byte[]> GenerateTaxFilingReportAsync(int clientId, int taxYear, ReportFormat format)
    {
        try
        {
            var reportData = await _templateService.GetTaxFilingReportDataAsync(clientId, taxYear);

            return format switch
            {
                ReportFormat.PDF => await _reportGenerator.GeneratePdfReportAsync(reportData, "taxfiling"),
                ReportFormat.Excel => await _reportGenerator.GenerateExcelReportAsync(reportData, "taxfiling"),
                ReportFormat.CSV => await _reportGenerator.GenerateCsvReportAsync(reportData, "taxfiling"),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tax filing report for client {ClientId}, year {TaxYear}", clientId, taxYear);
            throw;
        }
    }

    public async Task<byte[]> GeneratePaymentHistoryReportAsync(int clientId, DateTime fromDate, DateTime toDate, ReportFormat format)
    {
        try
        {
            var reportData = await _templateService.GetPaymentHistoryReportDataAsync(clientId, fromDate, toDate);

            return format switch
            {
                ReportFormat.PDF => await _reportGenerator.GeneratePdfReportAsync(reportData, "paymenthistory"),
                ReportFormat.Excel => await _reportGenerator.GenerateExcelReportAsync(reportData, "paymenthistory"),
                ReportFormat.CSV => await _reportGenerator.GenerateCsvReportAsync(reportData, "paymenthistory"),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payment history report for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<byte[]> GenerateComplianceReportAsync(int clientId, DateTime fromDate, DateTime toDate, ReportFormat format)
    {
        try
        {
            var reportData = await _templateService.GetComplianceReportDataAsync(clientId, fromDate, toDate);

            return format switch
            {
                ReportFormat.PDF => await _reportGenerator.GeneratePdfReportAsync(reportData, "compliance"),
                ReportFormat.Excel => await _reportGenerator.GenerateExcelReportAsync(reportData, "compliance"),
                ReportFormat.CSV => await _reportGenerator.GenerateCsvReportAsync(reportData, "compliance"),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating compliance report for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<byte[]> GenerateClientActivityReportAsync(DateTime fromDate, DateTime toDate, ReportFormat format)
    {
        try
        {
            var reportData = await _templateService.GetClientActivityReportDataAsync(fromDate, toDate);

            return format switch
            {
                ReportFormat.PDF => await _reportGenerator.GeneratePdfReportAsync(reportData, "clientactivity"),
                ReportFormat.Excel => await _reportGenerator.GenerateExcelReportAsync(reportData, "clientactivity"),
                ReportFormat.CSV => await _reportGenerator.GenerateCsvReportAsync(reportData, "clientactivity"),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating client activity report");
            throw;
        }
    }

    public async Task<byte[]> GenerateFinancialSummaryReportAsync(int? clientId, DateTime fromDate, DateTime toDate, ReportFormat format)
    {
        try
        {
            // Create a basic financial summary report
            var reportData = new ReportDataDto
            {
                Title = "Financial Summary Report",
                Subtitle = clientId.HasValue ? $"Client ID: {clientId}" : "All Clients",
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = "System",
                Data = new Dictionary<string, object>
                {
                    ["FromDate"] = fromDate.ToString("yyyy-MM-dd"),
                    ["ToDate"] = toDate.ToString("yyyy-MM-dd"),
                    ["ClientId"] = clientId?.ToString() ?? "All"
                }
            };

            return format switch
            {
                ReportFormat.PDF => await _reportGenerator.GeneratePdfReportAsync(reportData, "financialsummary"),
                ReportFormat.Excel => await _reportGenerator.GenerateExcelReportAsync(reportData, "financialsummary"),
                ReportFormat.CSV => await _reportGenerator.GenerateCsvReportAsync(reportData, "financialsummary"),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating financial summary report");
            throw;
        }
    }

    // Mark a queued report as cancelled (used by controller cancel endpoint)
    public async Task<bool> MarkReportCancelledAsync(string requestId)
    {
        try
        {
            var reportRequest = await _context.ReportRequests
                .FirstOrDefaultAsync(rr => rr.RequestId == requestId);

            if (reportRequest == null)
                return false;

            // Do not overwrite terminal states
            if (reportRequest.Status == ReportStatus.Completed || reportRequest.Status == ReportStatus.Failed)
                return false;

            // If already cancelled, treat as success
            if (reportRequest.Status == ReportStatus.Cancelled)
                return true;

            reportRequest.Status = ReportStatus.Cancelled;
            reportRequest.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Report {RequestId} marked as cancelled", requestId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking report {RequestId} as cancelled", requestId);
            throw;
        }
    }
}

// Quartz.NET job for background report generation
[DisallowConcurrentExecution]
public class ReportGenerationJob : IJob
{
    private readonly ApplicationDbContext _context;
    private readonly IReportTemplateService _templateService;
    private readonly IReportGenerator _reportGenerator;
    private readonly ILogger<ReportGenerationJob> _logger;

    public ReportGenerationJob(
        ApplicationDbContext context,
        IReportTemplateService templateService,
        IReportGenerator reportGenerator,
        ILogger<ReportGenerationJob> logger)
    {
        _context = context;
        _templateService = templateService;
        _reportGenerator = reportGenerator;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var requestId = context.JobDetail.JobDataMap.GetString("RequestId");
        var userId = context.JobDetail.JobDataMap.GetString("UserId");

        if (string.IsNullOrEmpty(requestId) || string.IsNullOrEmpty(userId))
        {
            _logger.LogError("Invalid job parameters: RequestId={RequestId}, UserId={UserId}", requestId, userId);
            return;
        }

        try
        {
            _logger.LogInformation("Starting report generation for request {RequestId}", requestId);

            var reportRequest = await _context.ReportRequests
                .FirstOrDefaultAsync(rr => rr.RequestId == requestId);

            if (reportRequest == null)
            {
                _logger.LogError("Report request {RequestId} not found", requestId);
                return;
            }

            // Update status to processing
            reportRequest.Status = ReportStatus.Processing;
            await _context.SaveChangesAsync();

            // Generate report based on type
            byte[] reportData = await GenerateReportData(reportRequest);

            // Save report to file system
            var fileName = GenerateFileName(reportRequest);
            var filePath = Path.Combine("Reports", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            await File.WriteAllBytesAsync(filePath, reportData);

            // Update report request with completion details
            reportRequest.Status = ReportStatus.Completed;
            reportRequest.CompletedAt = DateTime.UtcNow;
            reportRequest.DownloadUrl = filePath;
            reportRequest.FileSizeBytes = reportData.Length;
            reportRequest.FileName = fileName;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Completed report generation for request {RequestId}, file size: {FileSize} bytes",
                requestId, reportData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report for request {RequestId}", requestId);

            // Update request with error status
            var reportRequest = await _context.ReportRequests
                .FirstOrDefaultAsync(rr => rr.RequestId == requestId);

            if (reportRequest != null)
            {
                reportRequest.Status = ReportStatus.Failed;
                reportRequest.ErrorMessage = ex.Message;
                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task<byte[]> GenerateReportData(ReportRequest request)
    {
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(request.Parameters) ?? new();
        var templateName = request.Type.ToString().ToLower();

        ReportDataDto reportData = request.Type switch
        {
            ReportType.TaxFiling => await _templateService.GetTaxFilingReportDataAsync(
                GetIntParameter(parameters, "clientId"),
                GetIntParameter(parameters, "taxYear")),

            ReportType.PaymentHistory => await _templateService.GetPaymentHistoryReportDataAsync(
                GetIntParameter(parameters, "clientId"),
                GetDateParameter(parameters, "fromDate"),
                GetDateParameter(parameters, "toDate")),

            ReportType.Compliance => await _templateService.GetComplianceReportDataAsync(
                GetIntParameter(parameters, "clientId"),
                GetDateParameter(parameters, "fromDate"),
                GetDateParameter(parameters, "toDate")),

            ReportType.ClientActivity => await _templateService.GetClientActivityReportDataAsync(
                GetDateParameter(parameters, "fromDate"),
                GetDateParameter(parameters, "toDate")),

            _ => throw new ArgumentException($"Unsupported report type: {request.Type}")
        };

        return request.Format switch
        {
            ReportFormat.PDF => await _reportGenerator.GeneratePdfReportAsync(reportData, templateName),
            ReportFormat.Excel => await _reportGenerator.GenerateExcelReportAsync(reportData, templateName),
            ReportFormat.CSV => await _reportGenerator.GenerateCsvReportAsync(reportData, templateName),
            _ => throw new ArgumentException($"Unsupported format: {request.Format}")
        };
    }

    private static string GenerateFileName(ReportRequest request)
    {
        var extension = request.Format switch
        {
            ReportFormat.PDF => "pdf",
            ReportFormat.Excel => "xlsx",
            ReportFormat.CSV => "csv",
            _ => "bin"
        };

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return $"{request.Type}_{timestamp}_{request.RequestId[..8]}.{extension}";
    }

    private static int GetIntParameter(Dictionary<string, object> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value))
        {
            if (value is JsonElement element && element.ValueKind == JsonValueKind.Number)
                return element.GetInt32();
            if (int.TryParse(value?.ToString(), out var intValue))
                return intValue;
        }
        throw new ArgumentException($"Missing or invalid parameter: {key}");
    }

    private static DateTime GetDateParameter(Dictionary<string, object> parameters, string key)
    {
        if (parameters.TryGetValue(key, out var value))
        {
            if (value is JsonElement element && element.ValueKind == JsonValueKind.String)
            {
                if (DateTime.TryParse(element.GetString(), out var dateValue))
                    return dateValue;
            }
            if (DateTime.TryParse(value?.ToString(), out var parsedDate))
                return parsedDate;
        }
        throw new ArgumentException($"Missing or invalid date parameter: {key}");
    }

    // New report generation methods
    public async Task<byte[]> GenerateDocumentSubmissionReportAsync(int clientId, DateTime fromDate, DateTime toDate, ReportFormat format)
    {
        try
        {
            var reportData = await _templateService.GetDocumentSubmissionReportDataAsync(clientId, fromDate, toDate);

            return format switch
            {
                ReportFormat.PDF => await _reportGenerator.GeneratePdfReportAsync(reportData, "DocumentSubmissionReport"),
                ReportFormat.Excel => await _reportGenerator.GenerateExcelReportAsync(reportData, "DocumentSubmissionReport"),
                ReportFormat.CSV => await _reportGenerator.GenerateCsvReportAsync(reportData, "DocumentSubmissionReport"),
                _ => throw new ArgumentException($"Unsupported report format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating document submission report for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<byte[]> GenerateTaxCalendarReportAsync(int? clientId, int taxYear, ReportFormat format)
    {
        try
        {
            var reportData = await _templateService.GetTaxCalendarReportDataAsync(clientId, taxYear);

            return format switch
            {
                ReportFormat.PDF => await _reportGenerator.GeneratePdfReportAsync(reportData, "TaxCalendarReport"),
                ReportFormat.Excel => await _reportGenerator.GenerateExcelReportAsync(reportData, "TaxCalendarReport"),
                ReportFormat.CSV => await _reportGenerator.GenerateCsvReportAsync(reportData, "TaxCalendarReport"),
                _ => throw new ArgumentException($"Unsupported report format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tax calendar report for client {ClientId}, year {TaxYear}", clientId, taxYear);
            throw;
        }
    }

    public async Task<byte[]> GenerateClientComplianceOverviewReportAsync(DateTime fromDate, DateTime toDate, ReportFormat format)
    {
        try
        {
            var reportData = await _templateService.GetClientComplianceOverviewReportDataAsync(fromDate, toDate);

            return format switch
            {
                ReportFormat.PDF => await _reportGenerator.GeneratePdfReportAsync(reportData, "ClientComplianceOverviewReport"),
                ReportFormat.Excel => await _reportGenerator.GenerateExcelReportAsync(reportData, "ClientComplianceOverviewReport"),
                ReportFormat.CSV => await _reportGenerator.GenerateCsvReportAsync(reportData, "ClientComplianceOverviewReport"),
                _ => throw new ArgumentException($"Unsupported report format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating client compliance overview report");
            throw;
        }
    }

    public async Task<byte[]> GenerateRevenueReportAsync(DateTime fromDate, DateTime toDate, ReportFormat format)
    {
        try
        {
            var reportData = await _templateService.GetRevenueReportDataAsync(fromDate, toDate);

            return format switch
            {
                ReportFormat.PDF => await _reportGenerator.GeneratePdfReportAsync(reportData, "RevenueReport"),
                ReportFormat.Excel => await _reportGenerator.GenerateExcelReportAsync(reportData, "RevenueReport"),
                ReportFormat.CSV => await _reportGenerator.GenerateCsvReportAsync(reportData, "RevenueReport"),
                _ => throw new ArgumentException($"Unsupported report format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating revenue report");
            throw;
        }
    }

    public async Task<byte[]> GenerateCaseManagementReportAsync(DateTime fromDate, DateTime toDate, ReportFormat format)
    {
        try
        {
            var reportData = await _templateService.GetCaseManagementReportDataAsync(fromDate, toDate);

            return format switch
            {
                ReportFormat.PDF => await _reportGenerator.GeneratePdfReportAsync(reportData, "CaseManagementReport"),
                ReportFormat.Excel => await _reportGenerator.GenerateExcelReportAsync(reportData, "CaseManagementReport"),
                ReportFormat.CSV => await _reportGenerator.GenerateCsvReportAsync(reportData, "CaseManagementReport"),
                _ => throw new ArgumentException($"Unsupported report format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating case management report");
            throw;
        }
    }

    public async Task<byte[]> GenerateEnhancedClientActivityReportAsync(DateTime fromDate, DateTime toDate, string? clientFilter, string? activityTypeFilter, ReportFormat format)
    {
        try
        {
            var reportData = await _templateService.GetEnhancedClientActivityReportDataAsync(fromDate, toDate, clientFilter, activityTypeFilter);

            return format switch
            {
                ReportFormat.PDF => await _reportGenerator.GeneratePdfReportAsync(reportData, "EnhancedClientActivityReport"),
                ReportFormat.Excel => await _reportGenerator.GenerateExcelReportAsync(reportData, "EnhancedClientActivityReport"),
                ReportFormat.CSV => await _reportGenerator.GenerateCsvReportAsync(reportData, "EnhancedClientActivityReport"),
                _ => throw new ArgumentException($"Unsupported report format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating enhanced client activity report");
            throw;
        }
    }


}