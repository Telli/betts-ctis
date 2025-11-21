# Document Workflow Verification Report

**Date:** December 2024  
**Scope:** Verification of document workflow: required checklists per tax type, status transitions, versioning, virus scanning, retention rules (7 years)  
**Status:** COMPLETE

---

## Executive Summary

This report verifies the document workflow implementation including required checklists per tax type, status transitions, versioning, virus scanning, and retention rules. The system has comprehensive document requirements, versioning, and status tracking, but retention is set to 1 year instead of the required 7 years, and virus scanning is not production-ready.

**Overall Status:** ⚠️ **MOSTLY COMPLIANT** - Core features implemented, retention period and virus scanning need fixes

---

## Requirements

### Document Workflow Requirements

1. **Required Checklists Per Tax Type:** Document requirements defined for each tax type and taxpayer category
2. **Status Transitions:** Valid status transitions enforced
3. **Versioning:** Multiple versions of documents stored and tracked
4. **Virus Scanning:** Files scanned for malware before storage
5. **Retention Rules:** Documents retained for 7 years minimum

---

## Implementation Status

### 1. Required Checklists Per Tax Type

**File:** `BettsTax/BettsTax.Data/DocumentRequirement.cs`  
**Seeder:** `BettsTax/BettsTax.Data/DocumentRequirementSeeder.cs`

**Document Requirement Model:**
```csharp
public class DocumentRequirement
{
    public int DocumentRequirementId { get; set; }
    public string RequirementCode { get; set; }  // e.g., "PAYSLIP", "BANK_STATEMENT"
    public string Name { get; set; }  // e.g., "Employment Pay Slips"
    public string Description { get; set; }
    public TaxType? ApplicableTaxType { get; set; }
    public TaxpayerCategory? ApplicableTaxpayerCategory { get; set; }
    public bool IsRequired { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    
    // File validation rules
    public string AcceptedFormats { get; set; } = "pdf,jpg,jpeg,png,doc,docx";
    public long MaxFileSizeInBytes { get; set; } = 10485760;  // 10MB default
    public int MinimumQuantity { get; set; } = 1;
    public int MaximumQuantity { get; set; } = 10;
    public int? RequiredMonthsOfData { get; set; }
}
```

**Client Document Requirement Tracking:**
```csharp
public class ClientDocumentRequirement
{
    public int ClientDocumentRequirementId { get; set; }
    public int ClientId { get; set; }
    public int TaxFilingId { get; set; }
    public int DocumentRequirementId { get; set; }
    
    // Status tracking
    public DocumentVerificationStatus Status { get; set; } = DocumentVerificationStatus.NotRequested;
    public DateTime? RequestedDate { get; set; }
    public DateTime? FulfilledDate { get; set; }
    public DateTime? DueDate { get; set; }
    
    public string DocumentIds { get; set; } = string.Empty;  // Comma-separated list
    public int DocumentCount { get; set; } = 0;
}
```

**Document Requirements Seeded:**

**Personal Income Tax:**
- PIT_PAYSLIPS: Employment Pay Slips (12-24 months, required)
- PIT_BANK_STATEMENTS: Bank Statements (12-36 months, required)
- PIT_INVESTMENT_INCOME: Investment Income Statements (optional)
- PIT_RENTAL_INCOME: Rental Income Documentation (optional)
- PIT_TAX_CLEARANCE: Previous Year Tax Clearance Certificate (required)

**Corporate Income Tax:**
- CIT_FINANCIAL_STATEMENTS: Audited Financial Statements (required for Large taxpayers)
- CIT_TRIAL_BALANCE: General Ledger and Trial Balance (required for Large)
- CIT_DIRECTORS_REMUNERATION: Directors' Remuneration Schedule (required for Large)
- CIT_DEPRECIATION: Depreciation Schedules (required for Large)
- CIT_COMPANY_REGISTRATION: Company Registration Certificate (required for Large)

**GST:**
- GST_SALES_INVOICES: Sales Invoices and Records (required)
- GST_PURCHASE_INVOICES: Purchase Invoices and Receipts (required)
- GST_IMPORT_DOCS: Import Documentation (optional)
- GST_EXPORT_DOCS: Export Documentation (optional)
- GST_BANK_RECONCILIATION: Bank Reconciliation Statements (required)

**Payroll Tax (PAYE):**
- PAYE_PAYROLL: Employee Payroll Records (required)
- PAYE_NASSIT: NASSIT Contributions Schedule (required)
- PAYE_EMPLOYEE_CONTRACTS: Employee Contracts (optional)
- PAYE_TERMINATION_DOCS: Employee Termination Documentation (optional)

**General (All Tax Types):**
- GENERAL_TIN_CERT: TIN Certificate (required)
- GENERAL_NATIONAL_ID: National Identification (required)

**Analysis:**
- ✅ **COMPREHENSIVE REQUIREMENTS** - Requirements defined for all major tax types
- ✅ **TAXPAYER CATEGORY SPECIFIC** - Different requirements for Large/Medium/Small/Micro
- ✅ **QUANTITY RULES** - Minimum and maximum quantities defined
- ✅ **FORMAT VALIDATION** - Accepted file formats specified
- ✅ **MONTHLY DATA REQUIREMENTS** - RequiredMonthsOfData for periodic documents (e.g., 12 months of bank statements)
- ✅ **STATUS TRACKING** - ClientDocumentRequirement tracks fulfillment status
- ✅ **DISPLAY ORDER** - Requirements ordered for checklist display

**Verification Result:** ✅ **COMPLIANT** - Document requirements comprehensively defined

**API Endpoint:**
**File:** `sierra-leone-ctis/components/tax-filing-form.tsx` (lines 148-163)
```typescript
const reqs = await DocumentService.getDocumentRequirements(
    String(watchedTaxType), 
    String(selectedClient.taxpayerCategory)
);
```

---

### 2. Status Transitions

**File:** `BettsTax/BettsTax.Data/DocumentVerification.cs`

**Document Verification Status:**
```csharp
public enum DocumentVerificationStatus
{
    NotRequested,     // Document not yet requested from client
    Requested,        // Document requested, awaiting submission
    Submitted,        // Document uploaded by client, pending review
    UnderReview,      // Being reviewed by Betts Firm staff
    Rejected,         // Document rejected, resubmission required
    Verified,         // Document approved and verified
    Filed             // Document included in tax filing
}
```

**Document Submission Status (Alternative Workflow):**
**File:** `BettsTax/BettsTax.Data/DocumentManagementWorkflow.cs`
```csharp
public enum DocumentSubmissionStatus
{
    Submitted = 0,
    UnderVerification = 1,
    VerificationPassed = 2,
    VerificationFailed = 3,
    UnderApproval = 4,
    Approved = 5,
    Rejected = 6,
    Resubmitted = 7
}
```

**Status Transition Logic:**
**File:** `BettsTax/BettsTax.Core/Services/DocumentVerificationService.cs` (lines 118-159)

```csharp
public async Task<Result<DocumentVerificationDto>> UpdateDocumentVerificationAsync(
    int documentId, DocumentVerificationUpdateDto dto)
{
    var verification = await _context.DocumentVerifications
        .FirstOrDefaultAsync(dv => dv.DocumentId == documentId);
    
    var oldStatus = verification.Status;
    verification.Status = dto.Status;  // ⚠️ NO VALIDATION
    
    // Create history entry
    await CreateHistoryEntryAsync(documentId, oldStatus, dto.Status, dto.ReviewNotes);
    
    // Send notification if rejected
    if (dto.Status == DocumentVerificationStatus.Rejected)
    {
        // ... notification logic ...
    }
}
```

**Analysis:**
- ✅ **MULTIPLE STATUS ENUMS** - Two status systems (DocumentVerificationStatus and DocumentSubmissionStatus)
- ✅ **STATUS HISTORY** - DocumentVerificationHistory tracks status changes
- ❌ **NO TRANSITION VALIDATION** - Status can be changed to any value without validation
- ❌ **NO ENFORCED WORKFLOW** - Can skip steps (e.g., Submitted → Verified without UnderReview)
- ✅ **AUDIT TRAIL** - Status changes logged with user ID and timestamp

**Valid Transitions (Expected):**
```
NotRequested → Requested → Submitted → UnderReview → Verified → Filed
                                       → Rejected → Resubmitted → UnderReview
```

**Actual Behavior:**
- Status can be changed to any value directly
- No validation of valid transitions
- Workflow can be bypassed

**Verification Result:** ⚠️ **PARTIAL COMPLIANCE** - Status tracking exists but transitions not enforced

**Required Fix:**
```csharp
private readonly Dictionary<DocumentVerificationStatus, HashSet<DocumentVerificationStatus>> _validTransitions = new()
{
    { DocumentVerificationStatus.NotRequested, new() { DocumentVerificationStatus.Requested } },
    { DocumentVerificationStatus.Requested, new() { DocumentVerificationStatus.Submitted } },
    { DocumentVerificationStatus.Submitted, new() { DocumentVerificationStatus.UnderReview } },
    { DocumentVerificationStatus.UnderReview, new() { DocumentVerificationStatus.Verified, DocumentVerificationStatus.Rejected } },
    { DocumentVerificationStatus.Rejected, new() { DocumentVerificationStatus.Resubmitted } },
    { DocumentVerificationStatus.Resubmitted, new() { DocumentVerificationStatus.UnderReview } },
    { DocumentVerificationStatus.Verified, new() { DocumentVerificationStatus.Filed } }
};

public async Task<Result<DocumentVerificationDto>> UpdateDocumentVerificationAsync(
    int documentId, DocumentVerificationUpdateDto dto)
{
    var verification = await _context.DocumentVerifications
        .FirstOrDefaultAsync(dv => dv.DocumentId == documentId);
    
    var oldStatus = verification.Status;
    
    // Validate transition
    if (!_validTransitions.ContainsKey(oldStatus) || 
        !_validTransitions[oldStatus].Contains(dto.Status))
    {
        return Result.Failure<DocumentVerificationDto>(
            $"Invalid status transition from {oldStatus} to {dto.Status}");
    }
    
    verification.Status = dto.Status;
    // ... rest of logic ...
}
```

---

### 3. Versioning

**File:** `BettsTax/BettsTax.Data/DocumentVersion.cs`

**Document Version Model:**
```csharp
public class DocumentVersion
{
    public int DocumentVersionId { get; set; }
    public int DocumentId { get; set; }
    public int VersionNumber { get; set; }
    
    // File metadata for this version
    public string OriginalFileName { get; set; }
    public string StoredFileName { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
    public string StoragePath { get; set; }
    public string? Checksum { get; set; }
    
    // Audit
    public string? UploadedById { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    // Soft delete for retention
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedById { get; set; }
}
```

**Version Creation:**
**File:** `BettsTax/BettsTax.Core/Services/DocumentService.cs` (lines 213-259)

```csharp
public async Task<DocumentVersionDto> AddVersionAsync(int documentId, IFormFile file, string userId)
{
    var document = await _context.Documents
        .Include(d => d.DocumentVerification)
        .FirstOrDefaultAsync(d => d.DocumentId == documentId);
    
    // Validate file
    if (!await ValidateFileAsync(file))
        throw new InvalidOperationException("File validation failed");
    
    // Save file using storage service
    var storagePath = await _fileStorageService.SaveFileAsync(file, file.FileName, subfolder);
    
    var newVersionNumber = (document.CurrentVersionNumber <= 0 ? 0 : document.CurrentVersionNumber) + 1;
    
    var version = new DocumentVersion
    {
        DocumentId = document.DocumentId,
        VersionNumber = newVersionNumber,
        OriginalFileName = file.FileName,
        StoredFileName = Path.GetFileName(storagePath),
        ContentType = file.ContentType,
        Size = file.Length,
        StoragePath = storagePath,
        UploadedById = userId,
        UploadedAt = DateTime.UtcNow,
        IsDeleted = false
    };
    
    _context.DocumentVersions.Add(version);
    
    // Mirror latest version onto Document for backward compatibility
    document.OriginalFileName = version.OriginalFileName;
    document.StoredFileName = version.StoredFileName;
    document.ContentType = version.ContentType;
    document.Size = version.Size;
    document.StoragePath = version.StoragePath;
    document.CurrentVersionNumber = newVersionNumber;
    
    await _context.SaveChangesAsync();
}
```

**Alternative Version Control:**
**File:** `BettsTax/BettsTax.Data/DocumentManagementWorkflow.cs` (lines 144-178)

```csharp
public class DocumentVersionControl
{
    public Guid Id { get; set; }
    public int DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public string FileHash { get; set; }  // SHA256 hash for integrity
    public string? ChangeDescription { get; set; }
    public bool IsActive { get; set; } = true;
}
```

**Analysis:**
- ✅ **VERSION NUMBERING** - Automatic version numbering (1, 2, 3...)
- ✅ **VERSION HISTORY** - All versions stored and tracked
- ✅ **FILE STORAGE** - Each version stored separately
- ✅ **CHECKSUM/HASH** - DocumentVersionControl includes SHA256 hash
- ✅ **AUDIT TRAIL** - UploadedBy and UploadedAt tracked
- ✅ **SOFT DELETE** - Versions soft-deleted for retention
- ⚠️ **TWO SYSTEMS** - Both DocumentVersion and DocumentVersionControl exist (may cause confusion)
- ✅ **BACKWARD COMPATIBILITY** - Document entity mirrors latest version

**Verification Result:** ✅ **COMPLIANT** - Document versioning properly implemented

---

### 4. Virus Scanning

**File:** `BettsTax/BettsTax.Core/Services/FileStorageService.cs` (lines 129-318)

**Virus Scanning Implementation:**
```csharp
public async Task<bool> ScanFileForVirusAsync(string filePath)
{
    /*
     * PRODUCTION VIRUS SCANNING IMPLEMENTATION REQUIRED
     *
     * This is a placeholder implementation. For production deployment, integrate with a proper
     * antivirus solution. Recommended options:
     *
     * 1. ClamAV (Open Source, Cross-Platform)
     * 2. Windows Defender (Windows Only)
     * 3. Cloud-Based Scanning (VirusTotal, MetaDefender)
     * 4. Commercial Solutions (Sophos, McAfee, etc.)
     *
     * IMPORTANT: Fail-safe approach - if scanning fails, reject the file
     */
    
    try
    {
        _logger.LogWarning(
            "Using placeholder virus scanning for file {FilePath}. " +
            "PRODUCTION DEPLOYMENT REQUIRES PROPER ANTIVIRUS INTEGRATION!",
            filePath);
        
        // Basic file validation checks (NOT a substitute for real antivirus)
        var fileInfo = new FileInfo(filePath);
        
        // Check if file is too large (suspicious)
        if (fileInfo.Length > 100 * 1024 * 1024)  // 100MB
        {
            _logger.LogWarning("File {FilePath} is suspiciously large: {Size} bytes", filePath, fileInfo.Length);
            return false;
        }
        
        // Check file magic numbers (file signatures) to detect file type mismatches
        if (!await ValidateFileMagicNumbersAsync(filePath))
        {
            _logger.LogWarning("File {FilePath} has suspicious or mismatched file signature", filePath);
            return false;
        }
        
        // Check for executable file signatures
        if (await ContainsExecutableSignaturesAsync(filePath))
        {
            _logger.LogWarning("File {FilePath} contains executable signatures", filePath);
            return false;
        }
        
        _logger.LogDebug("File {FilePath} passed basic validation checks (NOT comprehensive virus scan)", filePath);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error scanning file {FilePath} for viruses", filePath);
        // Fail-safe: reject file if scanning fails
        return false;
    }
}
```

**Magic Number Validation:**
```csharp
private async Task<bool> ValidateFileMagicNumbersAsync(string filePath)
{
    using var stream = File.OpenRead(filePath);
    var buffer = new byte[8];
    var bytesRead = await stream.ReadAsync(buffer);
    
    // Check for common file signatures
    var extension = Path.GetExtension(filePath).ToLowerInvariant();
    
    // PDF: %PDF (25 50 44 46)
    if (extension == ".pdf")
        return buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46;
    
    // PNG, JPEG, ZIP/DOCX/XLSX checks...
    // ...
}
```

**Executable Signature Detection:**
```csharp
private async Task<bool> ContainsExecutableSignaturesAsync(string filePath)
{
    // Check for Windows executable signatures (MZ header)
    // Check for Linux executables (ELF header)
    // Check for macOS executables (Mach-O header)
    // Check for script-based malware patterns
    // ...
}
```

**Analysis:**
- ❌ **NOT PRODUCTION-READY** - Placeholder implementation only
- ✅ **BASIC VALIDATION** - File type validation, magic number checks, executable detection
- ✅ **FAIL-SAFE** - Rejects file if scanning fails
- ❌ **NO REAL VIRUS SCANNING** - No integration with antivirus software
- ⚠️ **DOCUMENTATION** - Comments guide production implementation

**Integration Guide:**
**File:** `BettsTax/VIRUS_SCANNING_INTEGRATION.md` - Provides guidance for ClamAV, Windows Defender, VirusTotal integration

**Verification Result:** ❌ **NOT COMPLIANT** - Virus scanning not production-ready

**Required Fix:**
Integrate with ClamAV, Windows Defender, or cloud-based antivirus service (see VIRUS_SCANNING_INTEGRATION.md)

---

### 5. Retention Rules

**File:** `BettsTax/BettsTax.Core/Services/DocumentRetentionBackgroundService.cs`  
**Configuration:** `BettsTax/BettsTax.Core/Options/DocumentRetentionOptions.cs`

**Retention Options:**
```csharp
public class DocumentRetentionOptions
{
    // How long to keep versions by age
    public int RetentionDays { get; set; } = 365;  // ⚠️ 1 YEAR, NOT 7 YEARS
    // Minimum number of most-recent versions to keep per document
    public int KeepMinVersions { get; set; } = 3;
    // Grace period after soft delete before physical deletion
    public int PhysicalDeleteGraceDays { get; set; } = 30;
    // How many records to process per pass
    public int BatchSize { get; set; } = 200;
    // How often the job runs (minutes)
    public int IntervalMinutes { get; set; } = 60;
}
```

**Configuration (appsettings.json):**
```json
{
  "DocumentRetention": {
    "RetentionDays": 365,
    "KeepMinVersions": 3,
    "PhysicalDeleteGraceDays": 30,
    "BatchSize": 200,
    "IntervalMinutes": 60
  }
}
```

**Retention Background Service:**
```csharp
private async Task ProcessOnceAsync(CancellationToken ct)
{
    var now = DateTime.UtcNow;
    var retentionDays = Math.Max(0, _options.Value.RetentionDays);
    var keepMin = Math.Max(1, _options.Value.KeepMinVersions);
    var graceDays = Math.Max(0, _options.Value.PhysicalDeleteGraceDays);
    
    var retentionCutoff = now.AddDays(-retentionDays);
    var graceCutoff = now.AddDays(-graceDays);
    
    // Phase 1: Soft-delete old versions beyond keep-min policy
    var softCandidates = await db.DocumentVersions
        .Where(v => !v.IsDeleted && v.UploadedAt <= retentionCutoff)
        .Join(db.Documents,
              v => v.DocumentId,
              d => d.DocumentId,
              (v, d) => new { v, d })
        .Where(x => x.v.VersionNumber <= x.d.CurrentVersionNumber - keepMin)
        .OrderBy(x => x.v.UploadedAt)
        .Take(batchSize)
        .ToListAsync(ct);
    
    // Soft delete old versions
    foreach (var x in softCandidates)
    {
        x.v.IsDeleted = true;
        x.v.DeletedAt = now;
    }
    
    // Phase 2: Physically delete files for versions past grace period
    var purgeCandidates = await db.DocumentVersions
        .Where(v => v.IsDeleted && v.DeletedAt != null && v.DeletedAt <= graceCutoff)
        .OrderBy(v => v.DeletedAt)
        .Take(batchSize)
        .ToListAsync(ct);
    
    // Physically delete files
    foreach (var v in purgeCandidates)
    {
        await storage.DeleteFileAsync(v.StoragePath);
    }
}
```

**Analysis:**
- ❌ **RETENTION PERIOD TOO SHORT** - 365 days (1 year) instead of 7 years (2555 days)
- ✅ **RETENTION SERVICE** - Background service runs retention cleanup
- ✅ **TWO-PHASE DELETION** - Soft delete first, then physical delete after grace period
- ✅ **KEEP MIN VERSIONS** - Keeps at least 3 most recent versions
- ✅ **BATCH PROCESSING** - Processes in batches to avoid performance issues
- ✅ **CONFIGURABLE** - Retention period configurable via appsettings.json
- ❌ **NO TAX-TYPE SPECIFIC RULES** - Same retention for all document types

**Requirement:** Documents must be retained for 7 years minimum  
**Current:** 365 days (1 year)

**Verification Result:** ❌ **NOT COMPLIANT** - Retention period does not meet 7-year requirement

**Required Fix:**
```json
{
  "DocumentRetention": {
    "RetentionDays": 2555,  // 7 years (7 * 365)
    "KeepMinVersions": 3,
    "PhysicalDeleteGraceDays": 30,
    "BatchSize": 200,
    "IntervalMinutes": 60
  }
}
```

**Optional Enhancement:**
```csharp
public class DocumentRetentionOptions
{
    // Default retention (7 years)
    public int RetentionDays { get; set; } = 2555;
    
    // Tax-type specific retention (optional)
    public Dictionary<string, int> TaxTypeRetentionDays { get; set; } = new()
    {
        { "IncomeTax", 2555 },  // 7 years
        { "GST", 2555 },        // 7 years
        { "PayrollTax", 2555 }, // 7 years
        { "ExciseDuty", 2555 }  // 7 years
    };
}
```

---

## Summary Table

| Document Workflow Feature | Required | Implemented | Status |
|---------------------------|----------|--------------|--------|
| **Required Checklists Per Tax Type** | ✅ | ✅ | ✅ **COMPLIANT** |
| **Status Transitions** | ✅ | ⚠️ | ⚠️ **PARTIAL** |
| **Versioning** | ✅ | ✅ | ✅ **COMPLIANT** |
| **Virus Scanning** | ✅ | ❌ | ❌ **NOT COMPLIANT** |
| **Retention Rules (7 years)** | ✅ | ❌ | ❌ **NOT COMPLIANT** |

**Overall Compliance:** ⚠️ **~60% COMPLIANT** (2 fully compliant, 1 partial, 2 not compliant)

---

## Issues Found

### Issue 1: Retention Period Set to 1 Year Instead of 7 Years

**Severity:** 🔴 **CRITICAL**

**Problem:** Document retention is set to 365 days (1 year) instead of 2555 days (7 years)

**Impact:**
- Compliance violation (tax documents must be retained for 7 years)
- Legal liability if documents deleted prematurely
- Audit failures

**Fix Required:**
```json
{
  "DocumentRetention": {
    "RetentionDays": 2555  // 7 years
  }
}
```

---

### Issue 2: Virus Scanning Not Production-Ready

**Severity:** 🔴 **CRITICAL**

**Problem:** Virus scanning is a placeholder with only basic file validation

**Impact:**
- Security risk (malicious files can be uploaded)
- Potential malware infection
- Data breach risk

**Fix Required:**
- Integrate ClamAV, Windows Defender, or cloud-based antivirus
- See `BettsTax/VIRUS_SCANNING_INTEGRATION.md` for guidance

---

### Issue 3: Status Transitions Not Enforced

**Severity:** 🟡 **MEDIUM**

**Problem:** Document status can be changed to any value without validation

**Impact:**
- Workflow can be bypassed
- Incorrect status tracking
- Audit trail issues

**Fix Required:**
- Implement status transition validation
- Define valid transitions dictionary
- Enforce workflow rules

---

## Required Fixes

### Fix 1: Update Retention Period to 7 Years

**Update appsettings.json:**
```json
{
  "DocumentRetention": {
    "RetentionDays": 2555,  // 7 years (7 * 365)
    "KeepMinVersions": 3,
    "PhysicalDeleteGraceDays": 30,
    "BatchSize": 200,
    "IntervalMinutes": 60
  }
}
```

**Update DocumentRetentionOptions default:**
```csharp
public class DocumentRetentionOptions
{
    public int RetentionDays { get; set; } = 2555;  // 7 years default
    // ... rest of properties ...
}
```

---

### Fix 2: Implement Virus Scanning

**Option 1: ClamAV Integration**
```csharp
using nClam;

public async Task<bool> ScanFileForVirusAsync(string filePath)
{
    try
    {
        var clam = new ClamClient("localhost", 3310);
        var scanResult = await clam.SendAndScanFileAsync(filePath);
        
        if (scanResult.Result == ClamScanResults.Clean)
        {
            _logger.LogDebug("File {FilePath} passed virus scan", filePath);
            return true;
        }
        
        _logger.LogWarning("File {FilePath} failed virus scan: {Result}", filePath, scanResult.Result);
        return false;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error scanning file {FilePath} for viruses", filePath);
        // Fail-safe: reject file if scanning fails
        return false;
    }
}
```

**Option 2: Windows Defender Integration**
```csharp
public async Task<bool> ScanFileForVirusAsync(string filePath)
{
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-Command \"Start-MpScan -ScanPath '{filePath}' -ScanType CustomScan\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };
        
        var process = Process.Start(psi);
        await process.WaitForExitAsync();
        
        return process.ExitCode == 0;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error scanning file {FilePath} for viruses", filePath);
        return false;
    }
}
```

---

### Fix 3: Enforce Status Transitions

**Add Validation to DocumentVerificationService:**
```csharp
private readonly Dictionary<DocumentVerificationStatus, HashSet<DocumentVerificationStatus>> _validTransitions = new()
{
    { DocumentVerificationStatus.NotRequested, new() { DocumentVerificationStatus.Requested } },
    { DocumentVerificationStatus.Requested, new() { DocumentVerificationStatus.Submitted } },
    { DocumentVerificationStatus.Submitted, new() { DocumentVerificationStatus.UnderReview } },
    { DocumentVerificationStatus.UnderReview, new() { DocumentVerificationStatus.Verified, DocumentVerificationStatus.Rejected } },
    { DocumentVerificationStatus.Rejected, new() { DocumentVerificationStatus.Resubmitted } },
    { DocumentVerificationStatus.Resubmitted, new() { DocumentVerificationStatus.UnderReview } },
    { DocumentVerificationStatus.Verified, new() { DocumentVerificationStatus.Filed } }
};

public async Task<Result<DocumentVerificationDto>> UpdateDocumentVerificationAsync(
    int documentId, DocumentVerificationUpdateDto dto)
{
    var verification = await _context.DocumentVerifications
        .FirstOrDefaultAsync(dv => dv.DocumentId == documentId);
    
    var oldStatus = verification.Status;
    
    // Validate transition
    if (!_validTransitions.ContainsKey(oldStatus) || 
        !_validTransitions[oldStatus].Contains(dto.Status))
    {
        return Result.Failure<DocumentVerificationDto>(
            $"Invalid status transition from {oldStatus} to {dto.Status}. " +
            $"Valid transitions: {string.Join(", ", _validTransitions[oldStatus])}");
    }
    
    verification.Status = dto.Status;
    // ... rest of update logic ...
}
```

---

## Testing Requirements

### Document Requirements Tests

1. **Tax Type Requirements Test:**
   - Query requirements for IncomeTax → Should return PIT requirements
   - Query requirements for GST → Should return GST requirements
   - Query requirements for Large taxpayer → Should return CIT requirements

2. **Client Checklist Test:**
   - Create tax filing → Should generate ClientDocumentRequirement entries
   - Upload document → Should update ClientDocumentRequirement status
   - Verify all required documents uploaded → Should calculate completion percentage

### Status Transition Tests

1. **Valid Transition Test:**
   - Change status from Submitted → UnderReview → Should succeed
   - Change status from Submitted → Verified → Should fail (must go through UnderReview)

2. **Invalid Transition Test:**
   - Attempt invalid transitions → Should return error
   - Verify error message includes valid transitions

### Versioning Tests

1. **Version Creation Test:**
   - Upload document → Should create version 1
   - Upload new version → Should create version 2
   - Verify both versions exist

2. **Version Retrieval Test:**
   - Get document → Should return latest version
   - Get specific version → Should return requested version

### Virus Scanning Tests

1. **Clean File Test:**
   - Upload clean PDF → Should pass scan
   - Verify file saved

2. **Malicious File Test:**
   - Upload executable → Should fail scan
   - Verify file rejected

### Retention Tests

1. **Retention Period Test:**
   - Create document → Wait 8 years → Should not be deleted
   - Verify document still exists

2. **Soft Delete Test:**
   - Old version beyond retention → Should be soft-deleted
   - Verify IsDeleted = true, DeletedAt set

3. **Physical Delete Test:**
   - Soft-deleted version past grace period → Should be physically deleted
   - Verify file removed from storage

---

## Recommendations

### Priority 1: Fix Retention Period
- Update RetentionDays to 2555 (7 years)
- Test retention service with new period
- Verify documents are not deleted prematurely

### Priority 2: Implement Virus Scanning
- Integrate ClamAV or Windows Defender
- Test with clean and malicious files
- Ensure fail-safe behavior (reject on scan failure)

### Priority 3: Enforce Status Transitions
- Add transition validation
- Define valid transitions per status
- Add unit tests for transition validation

### Priority 4: Tax-Type Specific Retention (Optional)
- Allow different retention periods per tax type
- Configure in DocumentRetentionOptions

---

**Report Generated:** December 2024  
**Next Steps:** Update retention period to 7 years and implement production-ready virus scanning

