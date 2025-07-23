using BettsTax.Core.DTOs;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    public interface IDataExportService
    {
        // Main export methods
        Task<Result<ExportResultDto>> ExportDataAsync(ExportRequestDto request);
        Task<Result<BulkExportResultDto>> BulkExportAsync(BulkExportRequestDto request);
        
        // Specific export methods
        Task<Result<ExportResultDto>> ExportTaxReturnsAsync(ExportRequestDto request);
        Task<Result<ExportResultDto>> ExportPaymentsAsync(ExportRequestDto request);
        Task<Result<ExportResultDto>> ExportClientsAsync(ExportRequestDto request);
        Task<Result<ExportResultDto>> ExportComplianceReportAsync(ExportRequestDto request);
        Task<Result<ExportResultDto>> ExportActivityLogAsync(ExportRequestDto request);
        Task<Result<ExportResultDto>> ExportDocumentsAsync(ExportRequestDto request);
        Task<Result<ExportResultDto>> ExportPenaltiesAsync(ExportRequestDto request);
        Task<Result<ExportResultDto>> ExportComprehensiveReportAsync(ExportRequestDto request);
        
        // Export history and management
        Task<Result<List<ExportHistoryDto>>> GetExportHistoryAsync(string? userId = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<Result<ExportResultDto>> GetExportByIdAsync(string exportId);
        Task<Result<bool>> DeleteExportAsync(string exportId);
        Task<Result<Stream>> DownloadExportAsync(string exportId, string? password = null);
        Task<Result<bool>> ExtendExportExpiryAsync(string exportId, DateTime newExpiryDate);
        
        // Export templates and scheduling
        Task<Result<List<ExportTemplateDto>>> GetExportTemplatesAsync();
        Task<Result<ExportTemplateDto>> CreateExportTemplateAsync(CreateExportTemplateDto template);
        Task<Result<ExportResultDto>> RunExportTemplateAsync(int templateId, DateTime? customStartDate = null, DateTime? customEndDate = null);
        
        // Validation and preview
        Task<Result<ExportPreviewDto>> PreviewExportAsync(ExportRequestDto request);
        Task<Result<bool>> ValidateExportRequestAsync(ExportRequestDto request);
        
        // Cleanup and maintenance
        Task<Result<int>> CleanupExpiredExportsAsync();
        Task<Result<ExportStatisticsDto>> GetExportStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    }

    public interface IExportFormatService
    {
        // Format-specific implementations
        Task<Result<string>> ExportToExcelAsync<T>(IEnumerable<T> data, string filePath, string sheetName = "Data");
        Task<Result<string>> ExportToCsvAsync<T>(IEnumerable<T> data, string filePath);
        Task<Result<string>> ExportToPdfAsync<T>(IEnumerable<T> data, string filePath, string title, ExportRequestDto request);
        Task<Result<string>> ExportToJsonAsync<T>(IEnumerable<T> data, string filePath);
        Task<Result<string>> ExportToXmlAsync<T>(IEnumerable<T> data, string filePath, string rootElement = "Data");
        
        // Multi-sheet Excel exports
        Task<Result<string>> ExportMultiSheetExcelAsync(Dictionary<string, object> sheets, string filePath);
        
        // PDF with custom formatting
        Task<Result<string>> ExportComprehensivePdfAsync(ComprehensiveExportData data, string filePath, ExportRequestDto request);
        
        // Utility methods
        Task<Result<string>> PasswordProtectFileAsync(string filePath, string password, ExportFormat format);
        Task<Result<string>> CompressFilesAsync(List<string> filePaths, string zipFilePath, string? password = null);
    }

    // Supporting DTOs
    public class ExportTemplateDto
    {
        public int TemplateId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ExportType ExportType { get; set; }
        public ExportFormat Format { get; set; }
        public string ConfigurationJson { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? LastRunDate { get; set; }
        public int RunCount { get; set; }
    }

    public class CreateExportTemplateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ExportRequestDto Configuration { get; set; } = new();
    }

    public class ExportPreviewDto
    {
        public int EstimatedRecordCount { get; set; }
        public long EstimatedFileSizeBytes { get; set; }
        public List<string> IncludedFields { get; set; } = new();
        public Dictionary<string, int> TableRecordCounts { get; set; } = new();
        public DateTime? DataStartDate { get; set; }
        public DateTime? DataEndDate { get; set; }
        public List<string> Warnings { get; set; } = new();
        public TimeSpan EstimatedProcessingTime { get; set; }
    }

    public class ExportStatisticsDto
    {
        public int TotalExports { get; set; }
        public long TotalSizeBytes { get; set; }
        public Dictionary<ExportType, int> ExportsByType { get; set; } = new();
        public Dictionary<ExportFormat, int> ExportsByFormat { get; set; } = new();
        public Dictionary<string, int> ExportsByUser { get; set; } = new();
        public int ActiveExports { get; set; }
        public int ExpiredExports { get; set; }
        public DateTime? OldestExport { get; set; }
        public DateTime? NewestExport { get; set; }
        public decimal AverageFileSizeMB { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
    }

    public class ComprehensiveExportData
    {
        public List<TaxReturnExportDto> TaxReturns { get; set; } = new();
        public List<PaymentExportDto> Payments { get; set; } = new();
        public List<ClientExportDto> Clients { get; set; } = new();
        public List<ComplianceReportExportDto> ComplianceReports { get; set; } = new();
        public List<ActivityLogExportDto> ActivityLogs { get; set; } = new();
        public List<DocumentExportDto> Documents { get; set; } = new();
        public List<PenaltyExportDto> Penalties { get; set; } = new();
        public ExportMetadataDto Metadata { get; set; } = new();
        public ExportRequestDto Request { get; set; } = new();
    }
}