using BettsTax.Core.DTOs.Reports;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services;

public partial class ReportService : IReportService
{
    // New report generation methods (moved into ReportService to satisfy interface)
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

