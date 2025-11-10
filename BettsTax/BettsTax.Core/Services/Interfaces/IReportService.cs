using BettsTax.Core.DTOs.Reports;
using BettsTax.Data;

namespace BettsTax.Core.Services.Interfaces;

public interface IReportService
{
    Task<string> QueueReportGenerationAsync(CreateReportRequestDto request, string userId);
    Task<ReportRequestDto?> GetReportStatusAsync(string requestId);
    Task<List<ReportRequestDto>> GetUserReportsAsync(string userId, int pageSize = 20, int pageNumber = 1);
    Task<int> GetUserReportCountAsync(string userId);
    Task<List<ReportRequestDto>> GetUserReportsAsync(string userId, ReportHistoryFilter filter, int pageSize = 20, int pageNumber = 1);
    Task<int> GetUserReportCountAsync(string userId, ReportHistoryFilter filter);
    Task<byte[]?> GetReportFileAsync(string requestId, string userId);
    Task<bool> DeleteReportAsync(string requestId, string userId);
    
    // Direct generation methods (for smaller reports that can be generated synchronously)
    Task<byte[]> GenerateTaxFilingReportAsync(int clientId, int taxYear, ReportFormat format);
    Task<byte[]> GeneratePaymentHistoryReportAsync(int clientId, DateTime fromDate, DateTime toDate, ReportFormat format);
    Task<byte[]> GenerateComplianceReportAsync(int clientId, DateTime fromDate, DateTime toDate, ReportFormat format);
    Task<byte[]> GenerateClientActivityReportAsync(DateTime fromDate, DateTime toDate, ReportFormat format);
    Task<byte[]> GenerateFinancialSummaryReportAsync(int? clientId, DateTime fromDate, DateTime toDate, ReportFormat format);
    
    // New report generation methods
    Task<byte[]> GenerateDocumentSubmissionReportAsync(int clientId, DateTime fromDate, DateTime toDate, ReportFormat format);
    Task<byte[]> GenerateTaxCalendarReportAsync(int? clientId, int taxYear, ReportFormat format);
    Task<byte[]> GenerateClientComplianceOverviewReportAsync(DateTime fromDate, DateTime toDate, ReportFormat format);
    Task<byte[]> GenerateRevenueReportAsync(DateTime fromDate, DateTime toDate, ReportFormat format);
    Task<byte[]> GenerateCaseManagementReportAsync(DateTime fromDate, DateTime toDate, ReportFormat format);
    Task<byte[]> GenerateEnhancedClientActivityReportAsync(DateTime fromDate, DateTime toDate, string? clientFilter, string? activityTypeFilter, ReportFormat format);
}

public interface IReportTemplateService
{
    Task<TaxFilingReportDataDto> GetTaxFilingReportDataAsync(int clientId, int taxYear);
    Task<PaymentHistoryReportDataDto> GetPaymentHistoryReportDataAsync(int clientId, DateTime fromDate, DateTime toDate);
    Task<ComplianceReportDataDto> GetComplianceReportDataAsync(int clientId, DateTime fromDate, DateTime toDate);
    Task<ClientActivityReportDataDto> GetClientActivityReportDataAsync(DateTime fromDate, DateTime toDate);
    
    // New report template methods
    Task<DocumentSubmissionReportDataDto> GetDocumentSubmissionReportDataAsync(int clientId, DateTime fromDate, DateTime toDate);
    Task<TaxCalendarReportDataDto> GetTaxCalendarReportDataAsync(int? clientId, int taxYear);
    Task<ClientComplianceOverviewReportDataDto> GetClientComplianceOverviewReportDataAsync(DateTime fromDate, DateTime toDate);
    Task<RevenueReportDataDto> GetRevenueReportDataAsync(DateTime fromDate, DateTime toDate);
    Task<CaseManagementReportDataDto> GetCaseManagementReportDataAsync(DateTime fromDate, DateTime toDate);
    Task<EnhancedClientActivityReportDataDto> GetEnhancedClientActivityReportDataAsync(DateTime fromDate, DateTime toDate, string? clientFilter, string? activityTypeFilter);
}

public interface IReportGenerator
{
    Task<byte[]> GeneratePdfReportAsync(ReportDataDto data, string templateName);
    Task<byte[]> GenerateExcelReportAsync(ReportDataDto data, string templateName);
    Task<byte[]> GenerateCsvReportAsync(ReportDataDto data, string templateName);
}