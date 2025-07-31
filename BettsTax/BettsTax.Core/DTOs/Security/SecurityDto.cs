using BettsTax.Data.Models.Security;

namespace BettsTax.Core.DTOs.Security;

// Multi-Factor Authentication DTOs
public class MfaSetupDto
{
    public bool IsEnabled { get; set; }
    public MfaMethod PreferredMethod { get; set; }
    public bool IsTotpEnabled { get; set; }
    public bool IsSmsEnabled { get; set; }
    public bool IsEmailEnabled { get; set; }
    public bool IsBackupCodesEnabled { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

public class MfaSetupResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? TotpSecret { get; set; }
    public string? QrCodeUrl { get; set; }
    public List<string>? BackupCodes { get; set; }
}

public class MfaChallengeRequestDto
{
    public MfaMethod Method { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

public class MfaChallengeDto
{
    public string ChallengeId { get; set; } = string.Empty;
    public MfaMethod Method { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public MfaChallengeStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int AttemptsRemaining { get; set; }
    public string? Message { get; set; }
}

public class MfaVerificationDto
{
    public string ChallengeId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool RememberDevice { get; set; }
}

public class MfaVerificationResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AccessToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsDeviceRemembered { get; set; }
}

// Audit Trail DTOs
public class AuditLogDto
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public AuditOperation Operation { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Changes { get; set; }
    public string? Description { get; set; }
    public AuditSeverity Severity { get; set; }
    public string SeverityName { get; set; } = string.Empty;
    public AuditCategory Category { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsComplianceRelevant { get; set; }
    public bool IsSecurityEvent { get; set; }
    public string? ComplianceTag { get; set; }
}

public class AuditSearchCriteriaDto
{
    public string? UserId { get; set; }
    public string? Action { get; set; }
    public string? Entity { get; set; }
    public AuditOperation? Operation { get; set; }
    public AuditSeverity? MinSeverity { get; set; }
    public AuditCategory? Category { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? IpAddress { get; set; }
    public bool? ComplianceRelevantOnly { get; set; }
    public bool? SecurityEventsOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AuditReportDto
{
    public DateTime GeneratedAt { get; set; }
    public AuditSearchCriteriaDto Criteria { get; set; } = new();
    public int TotalRecords { get; set; }
    public int ComplianceRelevantRecords { get; set; }
    public int SecurityEventRecords { get; set; }
    public Dictionary<string, int> ActionBreakdown { get; set; } = new();
    public Dictionary<string, int> SeverityBreakdown { get; set; } = new();
    public Dictionary<string, int> CategoryBreakdown { get; set; } = new();
    public List<AuditLogDto> Records { get; set; } = new();
}

// Security Event DTOs
public class SecurityEventDto
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string EventType { get; set; } = string.Empty;
    public SecurityEventSeverity Severity { get; set; }
    public string SeverityName { get; set; } = string.Empty;
    public SecurityEventCategory Category { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? EventData { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
    public int RiskScore { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsResolved { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public bool RequiresInvestigation { get; set; }
}

public class SecurityEventSearchDto
{
    public string? EventType { get; set; }
    public SecurityEventSeverity? MinSeverity { get; set; }
    public SecurityEventCategory? Category { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public bool? UnresolvedOnly { get; set; }
    public bool? RequiresInvestigation { get; set; }
    public int? MinRiskScore { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class SecurityEventResolutionDto
{
    public long EventId { get; set; }
    public string ResolutionNotes { get; set; } = string.Empty;
    public bool BlockUser { get; set; }
    public bool RequiresPasswordReset { get; set; }
    public bool RequiresMfaReset { get; set; }
}

// Data Encryption DTOs
public class EncryptionKeyDto
{
    public int Id { get; set; }
    public string KeyName { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public int KeySize { get; set; }
    public EncryptionKeyStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public EncryptionKeyType KeyType { get; set; }
    public string KeyTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime? LastRotatedAt { get; set; }
    public int RotationIntervalDays { get; set; }
    public bool AutoRotate { get; set; }
    public long UsageCount { get; set; }
    public int DaysUntilExpiration { get; set; }
    public bool RequiresRotation { get; set; }
}

public class CreateEncryptionKeyDto
{
    public string KeyName { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "AES-256";
    public int KeySize { get; set; } = 256;
    public EncryptionKeyType KeyType { get; set; }
    public string? Description { get; set; }
    public int ExpirationDays { get; set; } = 365;
    public int RotationIntervalDays { get; set; } = 90;
    public bool AutoRotate { get; set; } = true;
}

public class EncryptionStatisticsDto
{
    public int TotalKeys { get; set; }
    public int ActiveKeys { get; set; }
    public int ExpiredKeys { get; set; }
    public int KeysRequiringRotation { get; set; }
    public long TotalEncryptedRecords { get; set; }
    public Dictionary<string, int> KeyTypeBreakdown { get; set; } = new();
    public Dictionary<string, int> AlgorithmBreakdown { get; set; } = new();
    public List<EncryptionKeyDto> ExpiringKeys { get; set; } = new();
}

// System Monitoring DTOs
public class SystemHealthDto
{
    public string Component { get; set; } = string.Empty;
    public string CheckName { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? StatusMessage { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public double? ResponseTimeMs { get; set; }
    public long? MemoryUsageMB { get; set; }
    public double? CpuUsagePercent { get; set; }
    public long? ActiveConnections { get; set; }
    public bool IsWarning { get; set; }
    public bool IsCritical { get; set; }
}

public class SystemHealthOverviewDto
{
    public HealthStatus OverallStatus { get; set; }
    public string OverallStatusName { get; set; } = string.Empty;
    public DateTime LastCheckAt { get; set; }
    public int TotalComponents { get; set; }
    public int HealthyComponents { get; set; }
    public int WarningComponents { get; set; }
    public int CriticalComponents { get; set; }
    public List<SystemHealthDto> ComponentHealth { get; set; } = new();
    public List<SystemHealthDto> RecentIssues { get; set; } = new();
}

public class SecurityScanDto
{
    public long Id { get; set; }
    public string ScanType { get; set; } = string.Empty;
    public SecurityScanStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? InitiatedBy { get; set; }
    public int VulnerabilitiesFound { get; set; }
    public int HighRiskIssues { get; set; }
    public int MediumRiskIssues { get; set; }
    public int LowRiskIssues { get; set; }
    public string? Summary { get; set; }
    public bool RequiresAction { get; set; }
    public bool IsCompliant { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
}

public class StartSecurityScanDto
{
    public string ScanType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IncludeNetworkScan { get; set; }
    public bool IncludeVulnerabilityScan { get; set; }
    public bool IncludeComplianceScan { get; set; }
    public bool IncludePermissionAudit { get; set; }
}

public class SecurityDashboardDto
{
    public DateTime GeneratedAt { get; set; }
    public SystemHealthOverviewDto SystemHealth { get; set; } = new();
    public SecurityMetricsDto SecurityMetrics { get; set; } = new();
    public ComplianceStatusDto ComplianceStatus { get; set; } = new();
    public List<SecurityEventDto> RecentSecurityEvents { get; set; } = new();
    public List<SecurityScanDto> RecentScans { get; set; } = new();
}

public class SecurityMetricsDto
{
    public int ActiveUsers { get; set; }
    public int MfaEnabledUsers { get; set; }
    public double MfaAdoptionRate { get; set; }
    public int FailedLoginAttempts24h { get; set; }
    public int BlockedIpAddresses { get; set; }
    public int SecurityEvents24h { get; set; }
    public int HighRiskEvents24h { get; set; }
    public int EncryptedDataRecords { get; set; }
    public int AuditLogEntries24h { get; set; }
}

public class ComplianceStatusDto
{
    public bool IsCompliant { get; set; }
    public double ComplianceScore { get; set; }
    public int TotalChecks { get; set; }
    public int PassedChecks { get; set; }
    public int FailedChecksCount { get; set; }
    public List<ComplianceCheckDto> FailedChecks { get; set; } = new();
    public DateTime LastAssessment { get; set; }
    public DateTime NextAssessmentDue { get; set; }
}

public class ComplianceCheckDto
{
    public string CheckName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RecommendedAction { get; set; }
    public DateTime LastChecked { get; set; }
}

// User Security Profile DTOs
public class UserSecurityProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsMfaEnabled { get; set; }
    public MfaMethod PreferredMfaMethod { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public int FailedLoginAttempts { get; set; }
    public bool IsAccountLocked { get; set; }
    public DateTime? PasswordLastChanged { get; set; }
    public int DaysSincePasswordChange { get; set; }
    public bool RequiresPasswordReset { get; set; }
    public int SecurityEventsCount { get; set; }
    public int RiskScore { get; set; }
    public List<SecurityEventDto> RecentSecurityEvents { get; set; } = new();
    public List<AuditLogDto> RecentAuditLogs { get; set; } = new();
}

public class BulkSecurityActionDto
{
    public List<string> UserIds { get; set; } = new();
    public string Action { get; set; } = string.Empty; // "LOCK", "UNLOCK", "FORCE_MFA", "RESET_PASSWORD"
    public string? Reason { get; set; }
    public bool NotifyUsers { get; set; }
}