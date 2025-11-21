# Data Protection Security Audit Report

**Date:** December 2024  
**Scope:** Verification of data protection measures: file encryption at rest, database encryption for PII, TLS enforcement, PII masking in logs/exports, data residency compliance  
**Status:** COMPLETE

---

## Executive Summary

This report audits data protection measures across the Client Tax Information System (CTIS). The system implements field-level encryption for sensitive data and PII masking in audit logs, but lacks file encryption at rest, has gaps in PII masking for exports, and database encryption configuration needs verification.

**Overall Status:** ⚠️ **PARTIAL COMPLIANCE** - Core protections exist, critical gaps remain

---

## Requirements

### Data Protection Requirements

1. **File Encryption at Rest:** Encrypted storage for uploaded documents
2. **Database Encryption for PII:** Encrypted storage of personally identifiable information
3. **TLS Enforcement:** HTTPS/TLS required for all communications
4. **PII Masking:** Sensitive data masked in logs and exports
5. **Data Residency:** Compliance with geographic data location requirements

---

## Implementation Status

### 1. File Encryption at Rest

**File:** `BettsTax/BettsTax.Core/Services/FileStorageService.cs`

**Current Implementation:**
```csharp
public async Task<string> SaveFileAsync(IFormFile file, string fileName, string subfolder = "")
{
    // ... validation ...
    
    // Save file (ensure stream is closed before scanning)
    using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
    {
        await file.CopyToAsync(stream);
    }
    
    // Scan for virus (placeholder implementation) - after stream is disposed
    await ScanFileForVirusAsync(filePath);
    
    // Return relative path from base storage path
    return Path.GetRelativePath(_storageBasePath, filePath).Replace('\\', '/');
}
```

**Analysis:**
- ❌ **NOT ENCRYPTED** - Files saved as plain text on disk
- ❌ **NO ENCRYPTION SERVICE** - No encryption applied before saving
- ✅ **VIRUS SCANNING** - Placeholder for virus scanning (not production-ready)
- ✅ **SECURE FILE NAMES** - Uses timestamp + random string for file names
- ❌ **NO ENCRYPTION METADATA** - No tracking of encrypted vs unencrypted files

**Verification Result:** ❌ **NOT COMPLIANT** - Files stored unencrypted

**Security Impact:**
- If file system is compromised, all documents are readable
- Compliance violation (GDPR, data protection regulations)
- Risk of data breach if storage location is accessed

**Required Fix:**
```csharp
private readonly IEncryptionService _encryptionService;

public async Task<string> SaveFileAsync(IFormFile file, string fileName, string subfolder = "")
{
    // ... validation ...
    
    // Read file content
    byte[] fileContent;
    using (var memoryStream = new MemoryStream())
    {
        await file.CopyToAsync(memoryStream);
        fileContent = memoryStream.ToArray();
    }
    
    // Encrypt file content
    var encryptedContent = await _encryptionService.EncryptBytesAsync(
        fileContent, 
        "FileStorage");
    
    // Save encrypted file
    var filePath = Path.Combine(targetDirectory, secureFileName);
    await File.WriteAllBytesAsync(filePath, encryptedContent);
    
    // Store encryption metadata
    await StoreEncryptionMetadataAsync(secureFileName, "FileStorage");
    
    return Path.GetRelativePath(_storageBasePath, filePath);
}

public async Task<byte[]> GetFileAsync(string filePath)
{
    var encryptedContent = await File.ReadAllBytesAsync(fullPath);
    
    // Decrypt file content
    var decryptedContent = await _encryptionService.DecryptBytesAsync(
        encryptedContent,
        "FileStorage");
    
    return decryptedContent;
}
```

---

### 2. Database Encryption for PII

**File:** `BettsTax/BettsTax.Core/Services/Security/EncryptionService.cs`

**Field-Level Encryption Implementation:**
- ✅ **AES-256 ENCRYPTION** - Uses AES encryption with 256-bit keys
- ✅ **KEY MANAGEMENT** - Encryption keys stored in database (encrypted with master key)
- ✅ **KEY ROTATION** - Supports encryption key rotation
- ✅ **FIELD-LEVEL ENCRYPTION** - `EncryptFieldAsync` method for encrypting specific fields
- ✅ **PII CLASSIFICATION** - Marks fields as `IsPersonalData`, `IsFinancialData`, `IsComplianceData`

**Encryption Service Features:**
```csharp
public async Task<bool> EncryptFieldAsync(string entityType, string entityId, 
    string fieldName, string value, string keyName)
{
    var encryptedValue = await EncryptAsync(value, keyName);
    
    var encryptedData = new EncryptedData
    {
        EntityType = entityType,
        EntityId = entityId,
        FieldName = fieldName,
        EncryptedValue = encryptedValue,
        IsPersonalData = DetermineIfPersonalData(fieldName),
        IsFinancialData = DetermineIfFinancialData(fieldName),
        IsComplianceData = DetermineIfComplianceData(entityType, fieldName),
        DataClassification = ClassifyData(entityType, fieldName)
    };
    
    // ... save to database ...
}
```

**Database Connection String:**
**File:** `ops/production/config/appsettings.Production.template.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<db-host>;Port=5432;Database=<db-name>;Username=<db-user>;Password=<db-pass>;SslMode=Require;Trust Server Certificate=true"
  }
}
```

**Analysis:**
- ✅ **TLS IN TRANSIT** - `SslMode=Require` enforces TLS for database connections
- ⚠️ **TRUST SERVER CERTIFICATE** - `Trust Server Certificate=true` bypasses certificate validation (security risk)
- ✅ **FIELD-LEVEL ENCRYPTION SERVICE** - Available for encrypting PII fields
- ❌ **NOT APPLIED BY DEFAULT** - PII fields not automatically encrypted (manual application required)
- ❌ **NO DATABASE-LEVEL ENCRYPTION** - PostgreSQL TDE (Transparent Data Encryption) not configured

**Verification Result:** ⚠️ **PARTIAL COMPLIANCE** - Field-level encryption available but not enforced

**Issues:**
1. **Manual Encryption Required:** PII fields must be manually encrypted using `EncryptFieldAsync`
2. **No Automatic Encryption:** No automatic encryption for PII fields in entity models
3. **Certificate Validation Bypassed:** `Trust Server Certificate=true` disables certificate validation
4. **No Database TDE:** No transparent database encryption at storage level

**Required Fixes:**

**Fix 1: Enable Certificate Validation**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<db-host>;Port=5432;Database=<db-name>;Username=<db-user>;Password=<db-pass>;SslMode=Require;Trust Server Certificate=false;Root Certificate=<path-to-ca-cert>"
  }
}
```

**Fix 2: Automatically Encrypt PII Fields**
```csharp
// In ApplicationUser, Client, Payment entities
public class Client
{
    public int ClientId { get; set; }
    
    // Encrypted PII fields
    private string? _email;
    public string? Email 
    { 
        get => _email; 
        set 
        { 
            if (ShouldEncrypt(nameof(Email)))
            {
                _encryptedEmail = await _encryptionService.EncryptAsync(value, "PII");
            }
            else
            {
                _email = value;
            }
        }
    }
}
```

**Fix 3: Configure PostgreSQL TDE**
- Use PostgreSQL with TDE enabled
- Or use cloud database with encryption at rest (Azure SQL, AWS RDS with encryption)

---

### 3. TLS Enforcement

**File:** `BettsTax/BettsTax.Web/Program.cs` (lines 515-517)

**HTTPS Redirection:**
```csharp
app.UseHsts();
app.UseHttpsRedirection();
```

**HSTS Configuration:**
```csharp
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});
```

**Nginx Configuration:**
**File:** `BettsTax/nginx/nginx.conf` (lines 98-112, 115-134)

**HTTP to HTTPS Redirect:**
```nginx
server {
    listen 80 default_server;
    
    # Redirect all other traffic to HTTPS
    location / {
        return 301 https://$host$request_uri;
    }
}
```

**HTTPS Configuration:**
```nginx
server {
    listen 443 ssl http2 default_server;
    
    # SSL Configuration
    ssl_certificate /etc/nginx/ssl/fullchain.pem;
    ssl_certificate_key /etc/nginx/ssl/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    
    # HSTS
    add_header Strict-Transport-Security "max-age=63072000; includeSubDomains; preload" always;
}
```

**Email TLS/SSL:**
**File:** `BettsTax/BettsTax.Core/Services/EmailService.cs` (lines 147-148, 192-195)
```csharp
var useSSL = bool.Parse(emailSettings.GetValueOrDefault("Email.UseSSL", "true"));
var useTLS = bool.Parse(emailSettings.GetValueOrDefault("Email.UseTLS", "true"));

if (useSSL)
    secureSocketOptions = SecureSocketOptions.SslOnConnect;
else if (useTLS)
    secureSocketOptions = SecureSocketOptions.StartTls;
```

**Analysis:**
- ✅ **HTTPS REDIRECTION** - HTTP requests redirected to HTTPS
- ✅ **HSTS ENABLED** - HTTP Strict Transport Security configured (365 days, preload, includeSubDomains)
- ✅ **MODERN TLS** - TLS 1.2 and 1.3 only (no TLS 1.0/1.1)
- ✅ **EMAIL TLS** - SMTP uses TLS/SSL by default
- ✅ **NGINX HTTPS** - Production nginx configuration enforces HTTPS
- ⚠️ **NO CERTIFICATE VALIDATION** - Payment service SSL validation is basic (line 415-436 in PaymentEncryptionService.cs)

**Verification Result:** ✅ **COMPLIANT** - TLS enforcement properly configured

**Issues:**
- ⚠️ **Weak SSL Validation:** `ValidateSslCertificateAsync` only checks if request succeeds, not certificate validity

---

### 4. PII Masking in Logs

**File:** `BettsTax/BettsTax.Core/Services/Security/AuditService.cs`

**PII Masking Implementation:**
```csharp
// Sensitive fields that should be masked in audit logs
private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
{
    "password", "passwordhash", "token", "secret", "key", "pin", "ssn", "taxid",
    "accountnumber", "routingnumber", "creditcard", "cvv", "expiry"
};

private string? MaskSensitiveData(string? jsonData)
{
    if (string.IsNullOrEmpty(jsonData))
        return jsonData;

    try
    {
        var jsonDoc = JsonDocument.Parse(jsonData);
        var maskedData = MaskJsonElement(jsonDoc.RootElement);
        return JsonSerializer.Serialize(maskedData);
    }
    catch
    {
        // If JSON parsing fails, return as-is
        return jsonData;
    }
}

private object MaskJsonElement(JsonElement element)
{
    // ... masks fields in SensitiveFields list as "***MASKED***" ...
}
```

**Application in Audit Logging:**
```csharp
var auditLog = new AuditLog
{
    OldValues = MaskSensitiveData(oldValues),
    NewValues = MaskSensitiveData(newValues),
    // ...
};
```

**Additional Masking:**
**File:** `BettsTax/BettsTax.Core/Services/MfaService.cs` (lines 632-648)
```csharp
private string MaskPhoneNumber(string phoneNumber)
{
    if (phoneNumber.Length <= 4) return phoneNumber;
    return phoneNumber.Substring(0, 3) + "****" + phoneNumber.Substring(phoneNumber.Length - 2);
}

private string MaskEmail(string email)
{
    var parts = email.Split('@');
    if (parts.Length != 2) return email;
    
    var username = parts[0];
    if (username.Length <= 2) return email;
    return username.Substring(0, 2) + "****" + "@" + parts[1];
}
```

**Analysis:**
- ✅ **AUDIT LOG MASKING** - Sensitive fields masked in audit logs
- ✅ **JSON PARSING** - Handles JSON-structured data
- ✅ **FIELD LIST** - Comprehensive list of sensitive fields
- ⚠️ **INCOMPLETE FIELD LIST** - Missing email, phone, address fields from SensitiveFields list
- ⚠️ **NO SERILOG MASKING** - Serilog logs may contain unmasked PII

**Verification Result:** ⚠️ **PARTIAL COMPLIANCE** - Audit logs masked, but Serilog logs may leak PII

**Issues:**
1. **Email/Phone Not in SensitiveFields:** Email and phone number fields not automatically masked
2. **Serilog Not Masked:** Serilog logs (application logs) may contain unmasked PII
3. **Exception Messages:** Exception messages may contain PII (but error sanitization middleware handles this)

**Required Fix:**
```csharp
private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
{
    "password", "passwordhash", "token", "secret", "key", "pin", "ssn", "taxid",
    "accountnumber", "routingnumber", "creditcard", "cvv", "expiry",
    "email", "phone", "phonenumber", "address", "street", "postalcode",
    "bankaccount", "tin", "nationalid", "passport"
};
```

**Serilog Enricher for PII Masking:**
```csharp
public class PiiMaskingEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var properties = logEvent.Properties
            .Where(p => SensitiveFields.Contains(p.Key, StringComparer.OrdinalIgnoreCase))
            .ToList();
        
        foreach (var prop in properties)
        {
            logEvent.RemovePropertyIfPresent(prop.Key);
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                prop.Key, 
                "***MASKED***"));
        }
    }
}
```

---

### 5. PII Masking in Exports

**File:** `BettsTax/BettsTax.Core/Services/DataExportService.cs`

**Export Implementation:**
```csharp
var exportData = taxFilings.Select(t => new TaxReturnExportDto
{
    TaxFilingId = t.TaxFilingId,
    ClientNumber = t.Client?.ClientNumber ?? "",
    ClientName = t.Client?.BusinessName ?? "",
    TIN = t.Client?.TIN ?? "",
    // ... other fields ...
    ClientEmail = t.Client?.Email ?? "",  // ❌ UNMASKED
    ClientPhone = t.Client?.PhoneNumber ?? "",  // ❌ UNMASKED
    // ...
}).ToList();
```

**Analysis:**
- ❌ **UNMASKED PII IN EXPORTS** - Email and phone number exported in plain text
- ❌ **NO MASKING LOGIC** - No PII masking applied to export data
- ❌ **NO CONSENT CHECK** - No verification that export is authorized
- ✅ **PASSWORD PROTECTION** - Optional password protection for exports (if requested)
- ✅ **EXPORT LOGGING** - Export history logged with user ID

**Verification Result:** ❌ **NOT COMPLIANT** - PII exported without masking

**Security Impact:**
- Exported files may contain sensitive PII (email, phone, TIN)
- Compliance violation (GDPR, data protection)
- Risk if export files are lost or accessed by unauthorized users

**Required Fix:**
```csharp
public async Task<Result<ExportResultDto>> ExportTaxReturnsAsync(ExportRequestDto request)
{
    // ... existing code ...
    
    var exportData = taxFilings.Select(t => new TaxReturnExportDto
    {
        // ... other fields ...
        
        // Mask PII based on user permissions
        ClientEmail = await MaskPiiIfRequiredAsync(t.Client?.Email, "Email", request.RequestedBy),
        ClientPhone = await MaskPiiIfRequiredAsync(t.Client?.PhoneNumber, "Phone", request.RequestedBy),
        
        // Optionally mask TIN for non-admin exports
        TIN = request.RequestedByRole == "Admin" 
            ? t.Client?.TIN ?? "" 
            : MaskTin(t.Client?.TIN ?? ""),
    }).ToList();
    
    // ... rest of export ...
}

private string MaskTin(string tin)
{
    if (string.IsNullOrEmpty(tin) || tin.Length <= 4)
        return tin;
    
    return tin.Substring(0, 2) + "***" + tin.Substring(tin.Length - 2);
}

private async Task<string> MaskPiiIfRequiredAsync(string? value, string fieldType, string userId)
{
    // Check if user has permission to export unmasked PII
    var hasPermission = await _userContextService.HasPermissionAsync(
        userId, 
        "Export:UnmaskedPII");
    
    if (hasPermission || string.IsNullOrEmpty(value))
        return value ?? "";
    
    // Mask based on field type
    return fieldType switch
    {
        "Email" => MaskEmail(value),
        "Phone" => MaskPhoneNumber(value),
        _ => value
    };
}
```

---

### 6. Data Residency Compliance

**Files Searched:**
- `ops/production/ENVIRONMENT_SETUP.md`
- `BettsTax/docker-compose.prod.yml`
- Configuration files

**Findings:**
- ❌ **NO DATA RESIDENCY CONFIGURATION** - No explicit configuration for data location
- ❌ **NO GEOGRAPHIC RESTRICTIONS** - No restrictions on where data can be stored
- ⚠️ **CLOUD AGNOSTIC** - Mentions Azure Blob/S3 but no location enforcement
- ❌ **NO COMPLIANCE DOCUMENTATION** - No documentation of data residency requirements

**Analysis:**
- ⚠️ **NO ENFORCEMENT** - Data residency not enforced in code or configuration
- ❌ **NO DOCUMENTATION** - No documentation of where data should be stored
- ⚠️ **Sierra Leone Requirements:** Need to verify if data must be stored in Sierra Leone

**Verification Result:** ❌ **NOT COMPLIANT** - Data residency not configured or enforced

**Required Fix:**

**Add Data Residency Configuration:**
```json
{
  "DataResidency": {
    "RequiredRegion": "Sierra Leone",
    "AllowedRegions": ["sl-east-1", "sl-west-1"],
    "EnforceLocation": true,
    "StorageLocationPolicy": {
      "Database": "sl-east-1",
      "Files": "sl-east-1",
      "Backups": "sl-east-1"
    }
  }
}
```

**Add Validation Service:**
```csharp
public class DataResidencyService
{
    public async Task<bool> ValidateStorageLocationAsync(string resourceType, string location)
    {
        var allowedRegions = _configuration
            .GetSection("DataResidency:StorageLocationPolicy")
            .GetValue<string[]>(resourceType);
        
        return allowedRegions?.Contains(location) ?? false;
    }
}
```

---

## Summary Table

| Data Protection Measure | Required | Implemented | Status |
|-------------------------|----------|-------------|--------|
| **File Encryption at Rest** | ✅ | ❌ | ❌ **NOT COMPLIANT** |
| **Database Field-Level Encryption** | ✅ | ⚠️ | ⚠️ **PARTIAL** |
| **Database TLS in Transit** | ✅ | ✅ | ✅ **COMPLIANT** |
| **Database Certificate Validation** | ✅ | ⚠️ | ⚠️ **BYPASSED** |
| **Application TLS Enforcement** | ✅ | ✅ | ✅ **COMPLIANT** |
| **Email TLS/SSL** | ✅ | ✅ | ✅ **COMPLIANT** |
| **PII Masking in Audit Logs** | ✅ | ⚠️ | ⚠️ **PARTIAL** |
| **PII Masking in Serilog Logs** | ✅ | ❌ | ❌ **NOT COMPLIANT** |
| **PII Masking in Exports** | ✅ | ❌ | ❌ **NOT COMPLIANT** |
| **Data Residency Compliance** | ⚠️ | ❌ | ❌ **NOT COMPLIANT** |

**Overall Compliance:** ⚠️ **~50% COMPLIANT** (4 fully compliant, 3 partial, 4 not compliant)

---

## Critical Issues

### Issue 1: Files Not Encrypted at Rest

**Severity:** 🔴 **CRITICAL**

**Problem:** Uploaded documents stored in plain text on disk

**Impact:**
- Full data breach if file system compromised
- Compliance violation (GDPR, data protection laws)
- Legal liability for data exposure

**Fix Required:**
- Implement file encryption before saving
- Use EncryptionService to encrypt file content
- Store encryption metadata with files

---

### Issue 2: PII Exported Without Masking

**Severity:** 🔴 **CRITICAL**

**Problem:** Export service includes email, phone, TIN in plain text

**Impact:**
- PII exposed in exported files
- GDPR violation if exported data is lost
- Potential fines for non-compliance

**Fix Required:**
- Mask PII in exports based on user permissions
- Only admins should export unmasked PII
- Log all exports with PII access

---

### Issue 3: Serilog Logs May Contain Unmasked PII

**Severity:** 🟡 **HIGH**

**Problem:** Application logs (Serilog) may contain unmasked PII

**Impact:**
- PII exposed in log files
- Compliance violation
- Risk if logs are accessed

**Fix Required:**
- Implement Serilog enricher for PII masking
- Configure log sanitization
- Review all log statements for PII

---

### Issue 4: Database Certificate Validation Bypassed

**Severity:** 🟡 **HIGH**

**Problem:** `Trust Server Certificate=true` disables certificate validation

**Impact:**
- Vulnerable to man-in-the-middle attacks
- Database connection not properly authenticated

**Fix Required:**
- Remove `Trust Server Certificate=true`
- Provide proper CA certificate
- Enable certificate validation

---

### Issue 5: Data Residency Not Enforced

**Severity:** 🟡 **MEDIUM**

**Problem:** No enforcement of data location requirements

**Impact:**
- Potential violation of local data protection laws
- Regulatory compliance issues

**Fix Required:**
- Add data residency configuration
- Implement location validation
- Document compliance requirements

---

## Required Fixes

### Fix 1: Implement File Encryption at Rest

**Update FileStorageService:**
```csharp
private readonly IEncryptionService _encryptionService;

public async Task<string> SaveFileAsync(IFormFile file, string fileName, string subfolder = "")
{
    // ... validation ...
    
    // Read file content
    byte[] fileContent;
    using (var memoryStream = new MemoryStream())
    {
        await file.CopyToAsync(memoryStream);
        fileContent = memoryStream.ToArray();
    }
    
    // Encrypt file content
    var encryptedContent = await EncryptFileContentAsync(fileContent);
    
    // Save encrypted file
    var filePath = Path.Combine(targetDirectory, secureFileName);
    await File.WriteAllBytesAsync(filePath, encryptedContent);
    
    // Store encryption metadata
    await StoreFileEncryptionMetadataAsync(secureFileName, subfolder);
    
    return Path.GetRelativePath(_storageBasePath, filePath);
}

public async Task<byte[]> GetFileAsync(string filePath)
{
    var fullPath = Path.Combine(_storageBasePath, filePath);
    var encryptedContent = await File.ReadAllBytesAsync(fullPath);
    
    // Decrypt file content
    var decryptedContent = await DecryptFileContentAsync(encryptedContent);
    
    return decryptedContent;
}

private async Task<byte[]> EncryptFileContentAsync(byte[] content)
{
    // Convert to base64 string for encryption service
    var base64Content = Convert.ToBase64String(content);
    var encryptedString = await _encryptionService.EncryptAsync(base64Content, "FileStorage");
    return Convert.FromBase64String(encryptedString);
}

private async Task<byte[]> DecryptFileContentAsync(byte[] encryptedContent)
{
    var base64Encrypted = Convert.ToBase64String(encryptedContent);
    var decryptedString = await _encryptionService.DecryptAsync(base64Encrypted, "FileStorage");
    return Convert.FromBase64String(decryptedString);
}
```

---

### Fix 2: Mask PII in Exports

**Update DataExportService:**
```csharp
private async Task<string> MaskPiiIfRequiredAsync(string? value, string fieldType, string userId)
{
    if (string.IsNullOrEmpty(value))
        return "";
    
    // Check user permission
    var hasPermission = await _userContextService.HasPermissionAsync(
        userId, 
        "Export:UnmaskedPII");
    
    if (hasPermission)
        return value;
    
    // Mask based on field type
    return fieldType switch
    {
        "Email" => MaskEmail(value),
        "Phone" => MaskPhoneNumber(value),
        "TIN" => MaskTin(value),
        _ => value
    };
}

private string MaskTin(string tin)
{
    if (string.IsNullOrEmpty(tin) || tin.Length <= 4)
        return "***";
    return tin.Substring(0, 2) + "***" + tin.Substring(tin.Length - 2);
}
```

**Update Export DTOs:**
```csharp
ClientEmail = await MaskPiiIfRequiredAsync(t.Client?.Email, "Email", request.RequestedBy),
ClientPhone = await MaskPiiIfRequiredAsync(t.Client?.PhoneNumber, "Phone", request.RequestedBy),
TIN = request.RequestedByRole == "Admin" 
    ? t.Client?.TIN ?? "" 
    : MaskTin(t.Client?.TIN ?? ""),
```

---

### Fix 3: Mask PII in Serilog Logs

**Add Serilog Enricher:**
```csharp
public class PiiMaskingEnricher : ILogEventEnricher
{
    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passwordhash", "token", "secret", "key", "pin", "ssn", "taxid",
        "accountnumber", "creditcard", "email", "phone", "phonenumber", "address",
        "tin", "nationalid"
    };

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var propertiesToMask = logEvent.Properties
            .Where(p => SensitiveFields.Contains(p.Key, StringComparer.OrdinalIgnoreCase))
            .ToList();
        
        foreach (var prop in propertiesToMask)
        {
            logEvent.RemovePropertyIfPresent(prop.Key);
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                prop.Key, 
                "***MASKED***"));
        }
        
        // Also mask PII in message template if present
        if (logEvent.MessageTemplate?.Text != null)
        {
            // Simple regex-based masking (can be improved)
            var maskedMessage = Regex.Replace(
                logEvent.MessageTemplate.Text,
                @"\b[\w\.-]+@[\w\.-]+\.\w+\b", // Email
                "***@***.***",
                RegexOptions.IgnoreCase);
            
            maskedMessage = Regex.Replace(
                maskedMessage,
                @"\b\d{3}-\d{3}-\d{4}\b", // Phone
                "***-***-****",
                RegexOptions.IgnoreCase);
        }
    }
}
```

**Register in Program.cs:**
```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.With<PiiMaskingEnricher>()
        .WriteTo.Console()
        .WriteTo.File("logs/app.log");
});
```

---

### Fix 4: Enable Database Certificate Validation

**Update Connection String Template:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<db-host>;Port=5432;Database=<db-name>;Username=<db-user>;Password=<db-pass>;SslMode=Require;Trust Server Certificate=false;Root Certificate=/path/to/ca-cert.pem"
  }
}
```

**For Development:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=betts_tax;Username=betts_user;Password=password;SslMode=Prefer;Trust Server Certificate=false"
  }
}
```

---

### Fix 5: Add Data Residency Configuration

**Add Configuration:**
```json
{
  "DataResidency": {
    "RequiredRegion": "Sierra Leone",
    "AllowedRegions": ["sl-east-1", "sl-west-1"],
    "EnforceLocation": true,
    "StorageLocationPolicy": {
      "Database": "sl-east-1",
      "Files": "sl-east-1",
      "Backups": "sl-east-1",
      "Logs": "sl-east-1"
    }
  }
}
```

**Add Validation Service:**
```csharp
public class DataResidencyService : IDataResidencyService
{
    private readonly IConfiguration _configuration;
    
    public async Task<bool> ValidateStorageLocationAsync(string resourceType, string location)
    {
        var policy = _configuration.GetSection("DataResidency:StorageLocationPolicy");
        var allowedLocation = policy.GetValue<string>(resourceType);
        
        if (string.IsNullOrEmpty(allowedLocation))
            return false;
        
        return location.Equals(allowedLocation, StringComparison.OrdinalIgnoreCase);
    }
    
    public string GetRequiredLocationForResource(string resourceType)
    {
        return _configuration.GetValue<string>(
            $"DataResidency:StorageLocationPolicy:{resourceType}") ?? "sl-east-1";
    }
}
```

---

## Testing Requirements

### File Encryption Tests

1. **Encryption Test:**
   - Upload a file
   - Verify file on disk is encrypted (not readable as plain text)
   - Download file
   - Verify file content matches original

2. **Decryption Test:**
   - Retrieve encrypted file
   - Verify decryption works correctly
   - Verify file content integrity

### PII Masking Tests

1. **Export Masking Test:**
   - Export data as non-admin user
   - Verify email/phone/TIN are masked
   - Export as admin user
   - Verify PII is unmasked (if permission exists)

2. **Log Masking Test:**
   - Log message containing email/phone
   - Verify logs show masked values
   - Verify audit logs mask sensitive fields

3. **Audit Log Masking Test:**
   - Create audit log with sensitive data
   - Verify sensitive fields are masked
   - Verify non-sensitive fields are visible

### TLS Enforcement Tests

1. **HTTPS Redirect Test:**
   - Access HTTP endpoint
   - Verify redirect to HTTPS

2. **HSTS Test:**
   - Check response headers
   - Verify HSTS header present

3. **Database TLS Test:**
   - Connect to database without SSL
   - Verify connection is rejected
   - Verify connection with SSL succeeds

---

## Recommendations

### Priority 1: Fix File Encryption at Rest
- Implement file encryption in FileStorageService
- Test encryption/decryption
- Migrate existing files (encrypt on access)

### Priority 2: Mask PII in Exports
- Add masking logic to DataExportService
- Test exports with different user roles
- Document export permissions

### Priority 3: Mask PII in Serilog Logs
- Implement Serilog enricher
- Review all log statements
- Test log masking

### Priority 4: Enable Database Certificate Validation
- Update connection strings
- Provide CA certificates
- Test secure connections

### Priority 5: Configure Data Residency
- Document requirements
- Add configuration
- Implement validation

---

**Report Generated:** December 2024  
**Next Steps:** Implement file encryption, PII masking in exports, and Serilog enricher

