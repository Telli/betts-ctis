using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services.Security;

/// <summary>
/// Service for masking Personally Identifiable Information (PII) in data exports
/// Phase 3: PII Masking Implementation (fixes_plan.md §2.4)
/// </summary>
public interface IPiiMaskingService
{
    /// <summary>
    /// Mask PII fields in a dictionary of data
    /// </summary>
    Dictionary<string, object?> MaskPiiInDictionary(Dictionary<string, object?> data, PiiMaskingOptions? options = null);
    
    /// <summary>
    /// Mask PII fields in a list of dictionaries
    /// </summary>
    List<Dictionary<string, object?>> MaskPiiInList(List<Dictionary<string, object?>> dataList, PiiMaskingOptions? options = null);
    
    /// <summary>
    /// Mask PII in JSON string
    /// </summary>
    string MaskPiiInJson(string jsonData, PiiMaskingOptions? options = null);
    
    /// <summary>
    /// Mask a specific PII value based on field type
    /// </summary>
    string MaskValue(string value, PiiFieldType fieldType);
}

public class PiiMaskingService : IPiiMaskingService
{
    private readonly ILogger<PiiMaskingService> _logger;
    
    // PII field patterns - Phase 3 requirement
    private static readonly HashSet<string> PiiFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Names
        "firstname", "lastname", "fullname", "name", "contactname", "ownername",
        
        // Contact Information
        "email", "emailaddress", "phone", "phonenumber", "mobile", "telephone",
        "address", "streetaddress", "street", "city", "postalcode", "zipcode",
        
        // Identification Numbers
        "tin", "taxid", "taxidentificationnumber", "ssn", "socialsecurity",
        "nationalid", "idnumber", "passportnumber", "drivinglicense",
        
        // Financial Information
        "accountnumber", "bankaccount", "iban", "routingnumber",
        "creditcard", "cardnumber", "cvv", "expiry",
        
        // Sensitive Personal Data
        "dateofbirth", "dob", "birthdate", "age",
        "salary", "income", "compensation"
    };
    
    // Regex patterns for detecting PII in values
    private static readonly Regex EmailRegex = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"\b(\+?232[-.\s]?)?(\d{2}[-.\s]?\d{3}[-.\s]?\d{4}|\d{8})\b", RegexOptions.Compiled);
    private static readonly Regex TinRegex = new(@"\b\d{10}\b", RegexOptions.Compiled); // Sierra Leone TIN format

    public PiiMaskingService(ILogger<PiiMaskingService> logger)
    {
        _logger = logger;
    }

    public Dictionary<string, object?> MaskPiiInDictionary(Dictionary<string, object?> data, PiiMaskingOptions? options = null)
    {
        options ??= new PiiMaskingOptions();
        
        if (!options.Enabled)
            return data;

        var maskedData = new Dictionary<string, object?>();
        
        foreach (var kvp in data)
        {
            if (ShouldMaskField(kvp.Key, options))
            {
                maskedData[kvp.Key] = MaskFieldValue(kvp.Key, kvp.Value, options);
            }
            else
            {
                maskedData[kvp.Key] = kvp.Value;
            }
        }
        
        return maskedData;
    }

    public List<Dictionary<string, object?>> MaskPiiInList(List<Dictionary<string, object?>> dataList, PiiMaskingOptions? options = null)
    {
        options ??= new PiiMaskingOptions();
        
        if (!options.Enabled)
            return dataList;

        return dataList.Select(item => MaskPiiInDictionary(item, options)).ToList();
    }

    public string MaskPiiInJson(string jsonData, PiiMaskingOptions? options = null)
    {
        options ??= new PiiMaskingOptions();
        
        if (!options.Enabled || string.IsNullOrEmpty(jsonData))
            return jsonData;

        try
        {
            var jsonDoc = JsonDocument.Parse(jsonData);
            var maskedData = MaskJsonElement(jsonDoc.RootElement, options);
            return JsonSerializer.Serialize(maskedData, new JsonSerializerOptions { WriteIndented = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error masking PII in JSON data");
            return jsonData;
        }
    }

    public string MaskValue(string value, PiiFieldType fieldType)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return fieldType switch
        {
            PiiFieldType.Name => MaskName(value),
            PiiFieldType.Email => MaskEmail(value),
            PiiFieldType.Phone => MaskPhone(value),
            PiiFieldType.Address => MaskAddress(value),
            PiiFieldType.TaxId => MaskTaxId(value),
            PiiFieldType.BankAccount => MaskBankAccount(value),
            PiiFieldType.Generic => "***MASKED***",
            _ => value
        };
    }

    #region Private Helper Methods

    private bool ShouldMaskField(string fieldName, PiiMaskingOptions options)
    {
        // Check if field is in PII list
        if (PiiFieldNames.Contains(fieldName))
            return true;

        // Check custom fields to mask
        if (options.CustomFieldsToMask?.Any(f => f.Equals(fieldName, StringComparison.OrdinalIgnoreCase)) == true)
            return true;

        // Check excluded fields
        if (options.ExcludedFields?.Any(f => f.Equals(fieldName, StringComparison.OrdinalIgnoreCase)) == true)
            return false;

        return false;
    }

    private object? MaskFieldValue(string fieldName, object? value, PiiMaskingOptions options)
    {
        if (value == null)
            return null;

        var stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue))
            return value;

        // Determine field type and mask accordingly
        var fieldType = DetermineFieldType(fieldName);
        return MaskValue(stringValue, fieldType);
    }

    private PiiFieldType DetermineFieldType(string fieldName)
    {
        var lowerName = fieldName.ToLowerInvariant();
        
        if (lowerName.Contains("email"))
            return PiiFieldType.Email;
        if (lowerName.Contains("phone") || lowerName.Contains("mobile") || lowerName.Contains("telephone"))
            return PiiFieldType.Phone;
        if (lowerName.Contains("address") || lowerName.Contains("street") || lowerName.Contains("city"))
            return PiiFieldType.Address;
        if (lowerName.Contains("tin") || lowerName.Contains("taxid") || lowerName.Contains("ssn"))
            return PiiFieldType.TaxId;
        if (lowerName.Contains("account") || lowerName.Contains("bank") || lowerName.Contains("iban"))
            return PiiFieldType.BankAccount;
        if (lowerName.Contains("name"))
            return PiiFieldType.Name;
        
        return PiiFieldType.Generic;
    }

    private object MaskJsonElement(JsonElement element, PiiMaskingOptions options)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    var value = ShouldMaskField(property.Name, options)
                        ? MaskFieldValue(property.Name, property.Value.ToString(), options)
                        : MaskJsonElement(property.Value, options);
                    obj[property.Name] = value ?? "null";
                }
                return obj;

            case JsonValueKind.Array:
                return element.EnumerateArray().Select(e => MaskJsonElement(e, options)).ToArray();

            case JsonValueKind.String:
                return element.GetString() ?? "";

            case JsonValueKind.Number:
                return element.TryGetInt64(out var longValue) ? longValue : element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return "null";

            default:
                return element.ToString();
        }
    }

    // Specific masking strategies
    private string MaskName(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length <= 2)
            return "***";
        
        // Show first letter, mask rest
        return $"{name[0]}***";
    }

    private string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return "***@***.***";
        
        var parts = email.Split('@');
        var localPart = parts[0];
        var domain = parts[1];
        
        // Show first 2 chars of local part, mask rest
        var maskedLocal = localPart.Length > 2 
            ? $"{localPart.Substring(0, 2)}***" 
            : "***";
        
        // Show domain but mask subdomain
        var domainParts = domain.Split('.');
        var maskedDomain = domainParts.Length > 1
            ? $"***.{domainParts[^1]}"
            : "***";
        
        return $"{maskedLocal}@{maskedDomain}";
    }

    private string MaskPhone(string phone)
    {
        // Remove non-digits
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        
        if (digits.Length < 4)
            return "***";
        
        // Show last 4 digits
        return $"***-{digits.Substring(digits.Length - 4)}";
    }

    private string MaskAddress(string address)
    {
        if (string.IsNullOrEmpty(address) || address.Length <= 10)
            return "*** [REDACTED] ***";
        
        // Show first few words, mask rest
        var words = address.Split(' ');
        if (words.Length <= 2)
            return "*** [REDACTED] ***";
        
        return $"{words[0]} *** [REDACTED]";
    }

    private string MaskTaxId(string taxId)
    {
        // Remove non-alphanumeric
        var cleaned = new string(taxId.Where(char.IsLetterOrDigit).ToArray());
        
        if (cleaned.Length < 4)
            return "***";
        
        // Show last 4 characters
        return $"***{cleaned.Substring(cleaned.Length - 4)}";
    }

    private string MaskBankAccount(string account)
    {
        // Remove non-alphanumeric
        var cleaned = new string(account.Where(char.IsLetterOrDigit).ToArray());
        
        if (cleaned.Length < 4)
            return "***";
        
        // Show last 4 characters
        return $"***{cleaned.Substring(cleaned.Length - 4)}";
    }

    #endregion
}

/// <summary>
/// Options for PII masking configuration
/// </summary>
public class PiiMaskingOptions
{
    /// <summary>
    /// Enable/disable PII masking
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Custom fields to mask (in addition to default PII fields)
    /// </summary>
    public List<string>? CustomFieldsToMask { get; set; }
    
    /// <summary>
    /// Fields to exclude from masking
    /// </summary>
    public List<string>? ExcludedFields { get; set; }
    
    /// <summary>
    /// Masking level (partial, full)
    /// </summary>
    public MaskingLevel Level { get; set; } = MaskingLevel.Partial;
}

/// <summary>
/// PII field types for appropriate masking strategies
/// </summary>
public enum PiiFieldType
{
    Generic,
    Name,
    Email,
    Phone,
    Address,
    TaxId,
    BankAccount
}

/// <summary>
/// Masking level options
/// </summary>
public enum MaskingLevel
{
    /// <summary>
    /// Partial masking - show some characters
    /// </summary>
    Partial,
    
    /// <summary>
    /// Full masking - hide all characters
    /// </summary>
    Full
}
