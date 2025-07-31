using BettsTax.Data.Models.Security;
using BettsTax.Core.DTOs.Security;

namespace BettsTax.Core.Services.Interfaces;

// Multi-Factor Authentication Interface
public interface IMfaService
{
    Task<MfaSetupResultDto> SetupMfaAsync(string userId, MfaSetupDto setupDto);
    Task<MfaSetupDto?> GetMfaConfigurationAsync(string userId);
    Task<bool> DisableMfaAsync(string userId, string disabledBy);
    Task<MfaChallengeDto?> CreateChallengeAsync(string userId, MfaChallengeRequestDto request, string ipAddress, string userAgent);
    Task<MfaVerificationResultDto> VerifyChallengeAsync(string userId, MfaVerificationDto verification, string ipAddress, string userAgent);
    Task<bool> IsMfaEnabledAsync(string userId);
}

// Audit Service Interface
public interface IAuditService
{
    Task LogAsync(string? userId, string action, string entity, string? entityId, AuditOperation operation, 
        string? oldValues, string? newValues, string? description, AuditSeverity severity, AuditCategory category);
    Task LogLoginAttemptAsync(string? userId, string? username, bool success, string? failureReason, string ipAddress, string? userAgent);
    Task LogDataAccessAsync(string userId, string entity, string entityId, string action, Dictionary<string, object>? additionalData = null);
    Task LogDataModificationAsync(string userId, string entity, string entityId, object? oldData, object? newData, string operation);
    Task LogSecurityEventAsync(string? userId, string eventType, SecurityEventSeverity severity, SecurityEventCategory category, 
        string title, string? description, string? eventData);
    Task<(List<AuditLogDto> Records, int TotalCount)> SearchAuditLogsAsync(AuditSearchCriteriaDto criteria);
    Task<AuditReportDto> GenerateAuditReportAsync(AuditSearchCriteriaDto criteria);
    Task<List<SecurityEventDto>> GetSecurityEventsAsync(SecurityEventSearchDto criteria);
    Task<bool> ResolveSecurityEventAsync(SecurityEventResolutionDto resolution, string resolvedBy);
}

// Encryption Service Interface
public interface IEncryptionService
{
    Task<string> EncryptAsync(string plainText, string keyName);
    Task<string> DecryptAsync(string encryptedText, string keyName);
    Task<EncryptionKeyDto> CreateEncryptionKeyAsync(CreateEncryptionKeyDto request, string createdBy);
    Task<List<EncryptionKeyDto>> GetEncryptionKeysAsync(EncryptionKeyType? keyType = null, bool? activeOnly = true);
    Task<bool> RotateKeyAsync(int keyId, string rotatedBy);
    Task<bool> DeactivateKeyAsync(int keyId, string deactivatedBy);
    Task<EncryptionStatisticsDto> GetEncryptionStatisticsAsync();
    Task<bool> EncryptFieldAsync(string entityType, string entityId, string fieldName, string value, string keyName);
    Task<string?> DecryptFieldAsync(string entityType, string entityId, string fieldName);
    Task<byte[]> GenerateDigitalSignatureAsync(string data, string keyName);
    Task<bool> VerifyDigitalSignatureAsync(string data, byte[] signature, string keyName);
}

// System Monitoring Interface
public interface ISystemMonitoringService
{
    Task<SystemHealthOverviewDto> GetSystemHealthAsync();
    Task<List<SystemHealthDto>> GetComponentHealthAsync(string? component = null);
    Task LogHealthCheckAsync(string component, string checkName, HealthStatus status, string? statusMessage, 
        Dictionary<string, object>? checkData = null);
    Task<SecurityScanDto> StartSecurityScanAsync(StartSecurityScanDto request, string initiatedBy);
    Task<List<SecurityScanDto>> GetSecurityScansAsync(int page = 1, int pageSize = 20);
    Task<SecurityScanDto?> GetSecurityScanAsync(long scanId);
    Task<bool> ReviewSecurityScanAsync(long scanId, string reviewNotes, string reviewedBy);
    Task<SecurityDashboardDto> GetSecurityDashboardAsync();
}

// User Security Management Interface
public interface IUserSecurityService
{
    Task<UserSecurityProfileDto> GetUserSecurityProfileAsync(string userId);
    Task<List<UserSecurityProfileDto>> GetUsersRequiringAttentionAsync();
    Task<bool> LockUserAccountAsync(string userId, string reason, string lockedBy);
    Task<bool> UnlockUserAccountAsync(string userId, string reason, string unlockedBy);
    Task<bool> ForcePasswordResetAsync(string userId, string reason, string forcedBy);
    Task<bool> ForceMfaResetAsync(string userId, string reason, string forcedBy);
    Task<List<string>> BulkSecurityActionAsync(BulkSecurityActionDto request, string performedBy);
    Task<bool> UpdateUserRiskScoreAsync(string userId, int riskScore, string reason);
    Task<List<SecurityEventDto>> GetUserSecurityEventHistoryAsync(string userId, int days = 30);
}

// Compliance Monitoring Interface
public interface IComplianceMonitoringService
{
    Task<ComplianceStatusDto> GetComplianceStatusAsync();
    Task<List<ComplianceCheckDto>> RunComplianceChecksAsync();
    Task<bool> RecordComplianceViolationAsync(string violationType, string description, string affectedEntity, 
        string reportedBy, SecurityEventSeverity severity = SecurityEventSeverity.Medium);
    Task<List<ComplianceCheckDto>> GetFailedComplianceChecksAsync();
    Task<bool> ResolveComplianceIssueAsync(string checkName, string resolutionNotes, string resolvedBy);
    Task ScheduleComplianceAssessmentAsync(DateTime scheduledFor);
}

// Data Loss Prevention Interface
public interface IDataLossPreventionService
{
    Task<bool> ScanDataForSensitiveContentAsync(string content, string context);
    Task<List<string>> DetectSensitiveDataPatternsAsync(string content);
    Task<bool> ValidateDataExportRequestAsync(string userId, string dataType, string reason);
    Task LogDataAccessAttemptAsync(string userId, string dataType, string context, bool wasBlocked);
    Task<bool> ApproveDataAccessRequestAsync(string requestId, string approvedBy, string reason);
    Task<List<string>> GetBlockedDataAccessAttemptsAsync(DateTime fromDate, DateTime toDate);
}

// Threat Detection Interface
public interface IThreatDetectionService
{
    Task<int> AnalyzeUserBehaviorAsync(string userId, string action, Dictionary<string, object> context);
    Task<bool> DetectAnomalousActivityAsync(string userId, string activity, Dictionary<string, object> metadata);
    Task<List<SecurityEventDto>> GetActiveThreatsAsync();
    Task<bool> BlockSuspiciousActivityAsync(string userId, string reason, string blockedBy);
    Task<bool> WhitelistUserActivityAsync(string userId, string activityType, string reason, string authorizedBy);
    Task UpdateThreatIntelligenceAsync(Dictionary<string, object> threatData);
    Task<List<string>> GetBlockedIpAddressesAsync();
    Task<bool> BlockIpAddressAsync(string ipAddress, string reason, TimeSpan duration);
}