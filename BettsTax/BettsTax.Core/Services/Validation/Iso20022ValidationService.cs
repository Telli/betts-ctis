using System.Xml;
using System.Xml.Schema;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services.Validation;

public interface IIso20022ValidationService
{
    Task<ValidationResult> ValidatePaymentMessageAsync(string xmlContent, string messageType);
    Task<bool> LoadSchemasAsync();
}

public class Iso20022ValidationService : IIso20022ValidationService
{
    private readonly ILogger<Iso20022ValidationService> _logger;
    private readonly Dictionary<string, XmlSchemaSet> _schemaCache;
    private readonly string _schemaBasePath;

    public Iso20022ValidationService(ILogger<Iso20022ValidationService> logger)
    {
        _logger = logger;
        _schemaCache = new Dictionary<string, XmlSchemaSet>();
        _schemaBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas", "ISO20022");
    }

    public async Task<bool> LoadSchemasAsync()
    {
        try
        {
            // Load common ISO 20022 payment schemas
            await LoadSchemaAsync("pain.001.001.03", "pain.001.001.03.xsd"); // Customer Credit Transfer Initiation
            await LoadSchemaAsync("pain.002.001.03", "pain.002.001.03.xsd"); // Payment Status Report
            await LoadSchemaAsync("camt.053.001.02", "camt.053.001.02.xsd"); // Bank to Customer Statement
            await LoadSchemaAsync("camt.054.001.02", "camt.054.001.02.xsd"); // Bank to Customer Debit Credit Notification

            _logger.LogInformation("Successfully loaded {Count} ISO 20022 schemas", _schemaCache.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ISO 20022 schemas");
            return false;
        }
    }

    public async Task<ValidationResult> ValidatePaymentMessageAsync(string xmlContent, string messageType)
    {
        var result = new ValidationResult { IsValid = true, Errors = new List<string>() };

        try
        {
            if (!_schemaCache.ContainsKey(messageType))
            {
                result.IsValid = false;
                result.Errors.Add($"Schema not found for message type: {messageType}");
                return result;
            }

            var schemaSet = _schemaCache[messageType];
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schemaSet,
                ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema |
                                XmlSchemaValidationFlags.ReportValidationWarnings
            };

            settings.ValidationEventHandler += (sender, e) =>
            {
                if (e.Severity == XmlSeverityType.Error)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Validation Error: {e.Message} at line {e.Exception?.LineNumber}, position {e.Exception?.LinePosition}");
                }
                else if (e.Severity == XmlSeverityType.Warning)
                {
                    result.Warnings.Add($"Validation Warning: {e.Message}");
                }
            };

            using var stringReader = new StringReader(xmlContent);
            using var xmlReader = XmlReader.Create(stringReader, settings);

            // Read through the entire document to trigger validation
            while (await xmlReader.ReadAsync()) { }

            if (result.IsValid)
            {
                _logger.LogDebug("ISO 20022 message validation successful for type: {MessageType}", messageType);
            }
            else
            {
                _logger.LogWarning("ISO 20022 message validation failed for type: {MessageType}. Errors: {Errors}", 
                    messageType, string.Join("; ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Validation exception: {ex.Message}");
            _logger.LogError(ex, "Exception during ISO 20022 validation for message type: {MessageType}", messageType);
        }

        return result;
    }

    private async Task LoadSchemaAsync(string messageType, string schemaFileName)
    {
        var schemaPath = Path.Combine(_schemaBasePath, schemaFileName);
        
        if (!File.Exists(schemaPath))
        {
            _logger.LogWarning("Schema file not found: {SchemaPath}", schemaPath);
            return;
        }

        await Task.Run(() =>
        {
            var schemaSet = new XmlSchemaSet();
            
            using var schemaReader = XmlReader.Create(schemaPath);
            schemaSet.Add(null, schemaReader);
            schemaSet.Compile();

            _schemaCache[messageType] = schemaSet;
            
            _logger.LogDebug("Loaded schema for message type: {MessageType}", messageType);
        });
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
