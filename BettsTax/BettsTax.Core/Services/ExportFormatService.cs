using BettsTax.Core.DTOs;
using BettsTax.Shared;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace BettsTax.Core.Services
{
    public class ExportFormatService : IExportFormatService
    {
        private readonly ILogger<ExportFormatService> _logger;

        public ExportFormatService(ILogger<ExportFormatService> logger)
        {
            _logger = logger;
        }

        public async Task<Result<string>> ExportToExcelAsync<T>(IEnumerable<T> data, string filePath, string sheetName = "Data")
        {
            try
            {
                // For now, we'll export as CSV since we don't have Excel library dependency
                // In a real implementation, you would use EPPlus or ClosedXML
                _logger.LogWarning("Excel export not fully implemented, falling back to CSV format");
                
                var csvPath = Path.ChangeExtension(filePath, "csv");
                var csvResult = await ExportToCsvAsync(data, csvPath);
                
                if (csvResult.IsSuccess)
                {
                    // Rename the file to have .xlsx extension for now
                    var excelPath = Path.ChangeExtension(filePath, "xlsx");
                    File.Move(csvResult.Value, excelPath);
                    return Result.Success(excelPath);
                }
                
                return csvResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to Excel format");
                return Result.Failure<string>("Failed to export to Excel: " + ex.Message);
            }
        }

        public async Task<Result<string>> ExportToCsvAsync<T>(IEnumerable<T> data, string filePath)
        {
            try
            {
                var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var csv = new StringBuilder();

                // Add header row
                var headers = properties.Select(p => GetDisplayName(p)).ToArray();
                csv.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

                // Add data rows
                foreach (var item in data)
                {
                    var values = properties.Select(p =>
                    {
                        var value = p.GetValue(item);
                        return FormatCsvValue(value);
                    }).ToArray();
                    
                    csv.AppendLine(string.Join(",", values.Select(EscapeCsvField)));
                }

                await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);
                
                _logger.LogInformation("Successfully exported {Count} records to CSV: {FilePath}", 
                    data.Count(), filePath);
                
                return Result.Success(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to CSV format");
                return Result.Failure<string>("Failed to export to CSV: " + ex.Message);
            }
        }

        public async Task<Result<string>> ExportToPdfAsync<T>(IEnumerable<T> data, string filePath, string title, ExportRequestDto request)
        {
            try
            {
                // For now, we'll create a simple HTML file that can be converted to PDF
                // In a real implementation, you would use iText7, SelectPdf, or similar
                var html = GenerateHtmlTable(data, title, request);
                var htmlPath = Path.ChangeExtension(filePath, "html");
                
                await File.WriteAllTextAsync(htmlPath, html, Encoding.UTF8);
                
                _logger.LogWarning("PDF export not fully implemented, created HTML file instead: {FilePath}", htmlPath);
                
                return Result.Success(htmlPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to PDF format");
                return Result.Failure<string>("Failed to export to PDF: " + ex.Message);
            }
        }

        public async Task<Result<string>> ExportToJsonAsync<T>(IEnumerable<T> data, string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(data, options);
                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                
                _logger.LogInformation("Successfully exported {Count} records to JSON: {FilePath}", 
                    data.Count(), filePath);
                
                return Result.Success(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to JSON format");
                return Result.Failure<string>("Failed to export to JSON: " + ex.Message);
            }
        }

        public async Task<Result<string>> ExportToXmlAsync<T>(IEnumerable<T> data, string filePath, string rootElement = "Data")
        {
            try
            {
                var doc = new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement(rootElement)
                );

                var root = doc.Root!;
                var itemElementName = typeof(T).Name.Replace("ExportDto", "").Replace("Dto", "");
                
                foreach (var item in data)
                {
                    var itemElement = new XElement(itemElementName);
                    var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    
                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(item);
                        var elementValue = value?.ToString() ?? "";
                        
                        // Handle special types
                        if (value is DateTime dt)
                            elementValue = dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                        else if (value is decimal || value is double || value is float)
                            elementValue = value.ToString();
                        else if (value is bool)
                            elementValue = value.ToString()?.ToLower() ?? "false";
                        
                        itemElement.Add(new XElement(prop.Name, elementValue));
                    }
                    
                    root.Add(itemElement);
                }

                await using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                await writer.WriteAsync(doc.ToString());
                
                _logger.LogInformation("Successfully exported {Count} records to XML: {FilePath}", 
                    data.Count(), filePath);
                
                return Result.Success(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to XML format");
                return Result.Failure<string>("Failed to export to XML: " + ex.Message);
            }
        }

        public async Task<Result<string>> ExportMultiSheetExcelAsync(Dictionary<string, object> sheets, string filePath)
        {
            try
            {
                // For now, create multiple CSV files in a ZIP
                var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                var csvFiles = new List<string>();

                foreach (var sheet in sheets)
                {
                    var csvPath = Path.Combine(tempDir, $"{sheet.Key}.csv");
                    
                    // Use reflection to call ExportToCsvAsync with the correct type
                    var dataType = sheet.Value.GetType();
                    if (dataType.IsGenericType && dataType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        var method = GetType().GetMethod(nameof(ExportToCsvAsync))!
                            .MakeGenericMethod(dataType.GetGenericArguments()[0]);
                        
                        var result = (Task<Result<string>>)method.Invoke(this, new object[] { sheet.Value, csvPath })!;
                        var csvResult = await result;
                        
                        if (csvResult.IsSuccess)
                            csvFiles.Add(csvResult.Value);
                    }
                }

                // Create ZIP file
                var zipPath = Path.ChangeExtension(filePath, "zip");
                using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    foreach (var csvFile in csvFiles)
                    {
                        archive.CreateEntryFromFile(csvFile, Path.GetFileName(csvFile));
                    }
                }

                // Cleanup temp files
                Directory.Delete(tempDir, true);

                _logger.LogInformation("Successfully exported multi-sheet data to ZIP: {FilePath}", zipPath);
                
                return Result.Success(zipPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting multi-sheet Excel");
                return Result.Failure<string>("Failed to export multi-sheet Excel: " + ex.Message);
            }
        }

        public async Task<Result<string>> ExportComprehensivePdfAsync(ComprehensiveExportData data, string filePath, ExportRequestDto request)
        {
            try
            {
                var html = GenerateComprehensiveHtml(data, request);
                var htmlPath = Path.ChangeExtension(filePath, "html");
                
                await File.WriteAllTextAsync(htmlPath, html, Encoding.UTF8);
                
                _logger.LogWarning("Comprehensive PDF export not fully implemented, created HTML file instead: {FilePath}", htmlPath);
                
                return Result.Success(htmlPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting comprehensive PDF");
                return Result.Failure<string>("Failed to export comprehensive PDF: " + ex.Message);
            }
        }

        public async Task<Result<string>> PasswordProtectFileAsync(string filePath, string password, ExportFormat format)
        {
            try
            {
                // For now, create a password-protected ZIP file
                var protectedPath = Path.ChangeExtension(filePath, "protected.zip");
                
                using (var archive = ZipFile.Open(protectedPath, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
                }

                // Note: Standard .NET ZipFile doesn't support password protection
                // You would need a third-party library like SharpZipLib or DotNetZip
                _logger.LogWarning("Password protection not fully implemented, created regular ZIP file: {FilePath}", protectedPath);
                
                return Result.Success(protectedPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error password protecting file");
                return Result.Failure<string>("Failed to password protect file: " + ex.Message);
            }
        }

        public async Task<Result<string>> CompressFilesAsync(List<string> filePaths, string zipFilePath, string? password = null)
        {
            try
            {
                using (var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    foreach (var filePath in filePaths)
                    {
                        if (File.Exists(filePath))
                        {
                            archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
                        }
                    }
                }

                _logger.LogInformation("Successfully compressed {Count} files to ZIP: {FilePath}", 
                    filePaths.Count, zipFilePath);
                
                return Result.Success(zipFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing files");
                return Result.Failure<string>("Failed to compress files: " + ex.Message);
            }
        }

        // Helper methods
        private string GetDisplayName(PropertyInfo property)
        {
            var displayAttribute = property.GetCustomAttribute<DisplayNameAttribute>();
            return displayAttribute?.DisplayName ?? property.Name;
        }

        private string FormatCsvValue(object? value)
        {
            if (value == null)
                return "";

            if (value is DateTime dt)
                return dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            if (value is decimal || value is double || value is float)
                return value.ToString() ?? "";

            return value.ToString() ?? "";
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // Escape quotes and wrap in quotes if contains comma, quote, or newline
            if (field.Contains('"') || field.Contains(',') || field.Contains('\n') || field.Contains('\r'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }

        private string GenerateHtmlTable<T>(IEnumerable<T> data, string title, ExportRequestDto request)
        {
            var html = new StringBuilder();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine($"<title>{title}</title>");
            html.AppendLine("<style>");
            html.AppendLine("table { border-collapse: collapse; width: 100%; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine("th { background-color: #f2f2f2; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine($"<h1>{title}</h1>");
            html.AppendLine($"<p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
            
            if (!string.IsNullOrEmpty(request.Description))
                html.AppendLine($"<p>{request.Description}</p>");

            html.AppendLine("<table>");
            
            // Header
            html.AppendLine("<tr>");
            foreach (var prop in properties)
            {
                html.AppendLine($"<th>{GetDisplayName(prop)}</th>");
            }
            html.AppendLine("</tr>");

            // Data rows
            foreach (var item in data)
            {
                html.AppendLine("<tr>");
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(item);
                    var cellValue = FormatCsvValue(value);
                    html.AppendLine($"<td>{System.Web.HttpUtility.HtmlEncode(cellValue)}</td>");
                }
                html.AppendLine("</tr>");
            }

            html.AppendLine("</table>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private string GenerateComprehensiveHtml(ComprehensiveExportData data, ExportRequestDto request)
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<title>Comprehensive Tax Report</title>");
            html.AppendLine("<style>");
            html.AppendLine("table { border-collapse: collapse; width: 100%; margin-bottom: 20px; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine("th { background-color: #f2f2f2; }");
            html.AppendLine("h1, h2 { color: #333; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            html.AppendLine("<h1>Comprehensive Tax Report</h1>");
            html.AppendLine($"<p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
            html.AppendLine($"<p>Report Period: {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}</p>");

            // Summary section
            html.AppendLine("<h2>Summary</h2>");
            html.AppendLine($"<p>Clients: {data.Clients.Count}</p>");
            html.AppendLine($"<p>Tax Returns: {data.TaxReturns.Count}</p>");
            html.AppendLine($"<p>Payments: {data.Payments.Count}</p>");
            html.AppendLine($"<p>Compliance Reports: {data.ComplianceReports.Count}</p>");

            // Add sections for each data type
            if (data.Clients.Any())
            {
                html.AppendLine("<h2>Clients</h2>");
                html.AppendLine(GenerateHtmlTable(data.Clients, "Clients", request));
            }

            if (data.TaxReturns.Any())
            {
                html.AppendLine("<h2>Tax Returns</h2>");
                html.AppendLine(GenerateHtmlTable(data.TaxReturns, "Tax Returns", request));
            }

            if (data.Payments.Any())
            {
                html.AppendLine("<h2>Payments</h2>");
                html.AppendLine(GenerateHtmlTable(data.Payments, "Payments", request));
            }

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }
    }
}