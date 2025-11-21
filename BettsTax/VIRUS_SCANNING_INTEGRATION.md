# Virus Scanning Integration Guide

## Overview

The CTIS application currently uses a **placeholder virus scanning implementation** in `FileStorageService.cs`. This document provides guidance for integrating a production-ready antivirus solution.

## ⚠️ CRITICAL: Production Deployment Requirement

**The current implementation is NOT sufficient for production use.** It only performs basic file validation checks such as:
- File size limits
- File magic number validation (file signature verification)
- Detection of executable file signatures
- Basic pattern matching for suspicious content

**A proper antivirus solution MUST be integrated before production deployment.**

---

## Recommended Solutions

### Option 1: ClamAV (Recommended for Cross-Platform)

**Pros:**
- Open source and free
- Cross-platform (Windows, Linux, macOS)
- Well-maintained with regular virus definition updates
- Good performance for file scanning
- Can run as a daemon for fast scanning

**Cons:**
- Requires separate installation and configuration
- May have slightly lower detection rates than commercial solutions

**Implementation Steps:**

1. **Install ClamAV:**
   ```bash
   # Ubuntu/Debian
   sudo apt-get install clamav clamav-daemon
   
   # Windows
   # Download from https://www.clamav.net/downloads
   
   # macOS
   brew install clamav
   ```

2. **Start ClamAV Daemon:**
   ```bash
   # Linux
   sudo systemctl start clamav-daemon
   sudo systemctl enable clamav-daemon
   
   # Update virus definitions
   sudo freshclam
   ```

3. **Install NuGet Package:**
   ```bash
   dotnet add package nClam
   ```

4. **Update FileStorageService.cs:**
   ```csharp
   using nClam;
   
   public class FileStorageService : IFileStorageService
   {
       private readonly ClamClient _clamClient;
       
       public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
       {
           // ... existing code ...
           
           var clamHost = configuration["ClamAV:Host"] ?? "localhost";
           var clamPort = configuration.GetValue<int>("ClamAV:Port", 3310);
           _clamClient = new ClamClient(clamHost, clamPort);
       }
       
       public async Task<bool> ScanFileForVirusAsync(string filePath)
       {
           try
           {
               // Ping ClamAV to ensure it's running
               var pingResult = await _clamClient.PingAsync();
               if (!pingResult)
               {
                   _logger.LogError("ClamAV daemon is not responding");
                   return false; // Fail-safe: reject if scanner is down
               }
               
               // Scan the file
               var scanResult = await _clamClient.SendAndScanFileAsync(filePath);
               
               switch (scanResult.Result)
               {
                   case ClamScanResults.Clean:
                       _logger.LogInformation("File {FilePath} is clean", filePath);
                       return true;
                       
                   case ClamScanResults.VirusDetected:
                       _logger.LogWarning("Virus detected in file {FilePath}: {VirusName}", 
                           filePath, scanResult.InfectedFiles?.FirstOrDefault()?.VirusName);
                       return false;
                       
                   case ClamScanResults.Error:
                       _logger.LogError("Error scanning file {FilePath}: {RawResult}", 
                           filePath, scanResult.RawResult);
                       return false; // Fail-safe
                       
                   default:
                       _logger.LogWarning("Unknown scan result for file {FilePath}", filePath);
                       return false; // Fail-safe
               }
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Exception during virus scan of file {FilePath}", filePath);
               return false; // Fail-safe: reject if scanning fails
           }
       }
   }
   ```

5. **Add Configuration (appsettings.json):**
   ```json
   {
     "ClamAV": {
       "Host": "localhost",
       "Port": 3310
     }
   }
   ```

---

### Option 2: Windows Defender (Windows Only)

**Pros:**
- Built into Windows (no additional installation)
- Good detection rates
- Free

**Cons:**
- Windows only
- Requires elevated permissions
- Slower than daemon-based solutions

**Implementation:**

```csharp
using System.Diagnostics;

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
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        
        using var process = Process.Start(psi);
        if (process == null)
        {
            _logger.LogError("Failed to start Windows Defender scan");
            return false;
        }
        
        await process.WaitForExitAsync();
        
        if (process.ExitCode == 0)
        {
            _logger.LogInformation("File {FilePath} is clean", filePath);
            return true;
        }
        else
        {
            _logger.LogWarning("Windows Defender detected threat in file {FilePath}", filePath);
            return false;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error scanning file with Windows Defender");
        return false;
    }
}
```

---

### Option 3: Cloud-Based Scanning (VirusTotal, MetaDefender)

**Pros:**
- No local installation required
- Multiple antivirus engines
- Always up-to-date

**Cons:**
- Requires internet connectivity
- API rate limits
- Data privacy concerns (files uploaded to third party)
- Latency

**⚠️ Privacy Warning:** Only use for non-sensitive documents or with explicit user consent.

**Implementation (VirusTotal):**

1. **Install NuGet Package:**
   ```bash
   dotnet add package VirusTotalNet
   ```

2. **Implementation:**
   ```csharp
   using VirusTotalNet;
   
   public async Task<bool> ScanFileForVirusAsync(string filePath)
   {
       try
       {
           var apiKey = _configuration["VirusTotal:ApiKey"];
           var virusTotal = new VirusTotalNet.VirusTotal(apiKey);
           
           // Upload and scan file
           var fileInfo = new FileInfo(filePath);
           var scanResult = await virusTotal.ScanFileAsync(fileInfo);
           
           // Wait for scan to complete
           await Task.Delay(15000); // VirusTotal takes time to scan
           
           var report = await virusTotal.GetFileReportAsync(scanResult.Resource);
           
           if (report.Positives > 0)
           {
               _logger.LogWarning("File {FilePath} flagged by {Count} antivirus engines", 
                   filePath, report.Positives);
               return false;
           }
           
           return true;
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Error scanning file with VirusTotal");
           return false;
       }
   }
   ```

---

## Configuration Recommendations

### appsettings.json

```json
{
  "FileStorage": {
    "BasePath": "C:\\CTIS\\Uploads",
    "AllowedExtensions": [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png"],
    "MaxFileSizeBytes": 52428800,
    "VirusScanningEnabled": true,
    "VirusScanningProvider": "ClamAV"
  },
  "ClamAV": {
    "Host": "localhost",
    "Port": 3310,
    "Timeout": 30000
  }
}
```

---

## Testing

### Unit Tests

```csharp
[Fact]
public async Task ScanFileForVirusAsync_CleanFile_ReturnsTrue()
{
    // Arrange
    var testFilePath = CreateTestFile("clean.pdf");
    
    // Act
    var result = await _fileStorageService.ScanFileForVirusAsync(testFilePath);
    
    // Assert
    Assert.True(result);
}

[Fact]
public async Task ScanFileForVirusAsync_InfectedFile_ReturnsFalse()
{
    // Arrange
    var testFilePath = CreateEicarTestFile(); // EICAR test virus
    
    // Act
    var result = await _fileStorageService.ScanFileForVirusAsync(testFilePath);
    
    // Assert
    Assert.False(result);
}
```

### EICAR Test File

Use the EICAR test file to verify antivirus integration:
```
X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*
```

---

## Deployment Checklist

- [ ] Choose antivirus solution based on deployment environment
- [ ] Install and configure antivirus software
- [ ] Update FileStorageService.cs with production implementation
- [ ] Add configuration settings to appsettings.json
- [ ] Test with EICAR test file
- [ ] Test with real documents
- [ ] Configure monitoring and alerting for scan failures
- [ ] Document virus scanning in security compliance documentation
- [ ] Train operations team on antivirus maintenance

---

## Monitoring and Maintenance

1. **Monitor scan failures:** Set up alerts for repeated scan failures
2. **Update virus definitions:** Ensure automatic updates are enabled
3. **Performance monitoring:** Track scan times and optimize if needed
4. **Quarantine management:** Implement process for handling detected threats
5. **Audit logging:** Log all scan results for compliance

---

## Support and Resources

- **ClamAV Documentation:** https://docs.clamav.net/
- **nClam GitHub:** https://github.com/tekmaven/nClam
- **Windows Defender API:** https://docs.microsoft.com/en-us/windows/security/threat-protection/microsoft-defender-antivirus/
- **VirusTotal API:** https://developers.virustotal.com/reference

