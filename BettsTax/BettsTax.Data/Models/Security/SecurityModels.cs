using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data.Models.Security;

// Multi-Factor Authentication Models
public class UserMfaConfiguration
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;
    
    public bool IsEnabled { get; set; }
    public MfaMethod PreferredMethod { get; set; }
    public bool IsTotpEnabled { get; set; }
    public bool IsSmsEnabled { get; set; }
    public bool IsEmailEnabled { get; set; }
    public bool IsBackupCodesEnabled { get; set; }
    
    [MaxLength(100)]
    public string? PhoneNumber { get; set; }
    
    [MaxLength(255)]
    public string? Email { get; set; }
    
    public string? TotpSecret { get; set; } // Encrypted
    public string? BackupCodes { get; set; } // Encrypted JSON array
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    
    // Navigation properties
    public ICollection<MfaChallenge> MfaChallenges { get; set; } = new List<MfaChallenge>();
}

public class MfaChallenge
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;
    
    public int MfaConfigurationId { get; set; }
    
    [ForeignKey(nameof(MfaConfigurationId))]
    public UserMfaConfiguration MfaConfiguration { get; set; } = null!;
    
    [Required]
    [MaxLength(100)]
    public string ChallengeId { get; set; } = string.Empty;
    
    public MfaMethod Method { get; set; }
    public MfaChallengeStatus Status { get; set; }
    
    [MaxLength(10)]
    public string? Code { get; set; } // For SMS/Email verification
    
    [MaxLength(500)]
    public string? Challenge { get; set; } // Method-specific challenge data
    
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 3;
    
    [MaxLength(200)]
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
}

// Audit Trail Models
public class AuditLog
{
    [Key]
    public long Id { get; set; }
    
    [MaxLength(450)]
    public string? UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Entity { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? EntityId { get; set; }
    
    public AuditOperation Operation { get; set; }
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    public string? Changes { get; set; } // JSON diff
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public AuditSeverity Severity { get; set; }
    public AuditCategory Category { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string IpAddress { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(100)]
    public string? SessionId { get; set; }
    
    [MaxLength(100)]
    public string? RequestId { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    // Compliance and regulatory tracking
    public bool IsComplianceRelevant { get; set; }
    public bool IsSecurityEvent { get; set; }
    public bool RequiresReview { get; set; }
    
    [MaxLength(100)]
    public string? ComplianceTag { get; set; }
}

public class SecurityEvent
{
    [Key]
    public long Id { get; set; }
    
    [MaxLength(450)]
    public string? UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    public SecurityEventSeverity Severity { get; set; }
    public SecurityEventCategory Category { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    public string? EventData { get; set; } // JSON
    
    [Required]
    [MaxLength(200)]
    public string IpAddress { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    [MaxLength(100)]
    public string? SessionId { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    // Risk assessment
    public int RiskScore { get; set; } // 0-100
    public bool IsBlocked { get; set; }
    public bool IsResolved { get; set; }
    
    [MaxLength(450)]
    public string? ResolvedBy { get; set; }
    
    public DateTime? ResolvedAt { get; set; }
    
    [MaxLength(1000)]
    public string? ResolutionNotes { get; set; }
    
    // Automated response tracking
    public bool AutoBlocked { get; set; }
    public bool NotificationSent { get; set; }
    public bool RequiresInvestigation { get; set; }
}

// Data Encryption Models
public class EncryptionKey
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string KeyName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Algorithm { get; set; } = string.Empty;
    
    public int KeySize { get; set; }
    
    [Required]
    public string EncryptedKey { get; set; } = string.Empty; // Key encrypted with master key
    
    [Required]
    public string KeyHash { get; set; } = string.Empty; // For key verification
    
    public EncryptionKeyStatus Status { get; set; }
    public EncryptionKeyType KeyType { get; set; }
    
    [MaxLength(200)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    
    [MaxLength(450)]
    public string CreatedBy { get; set; } = string.Empty;
    
    public bool IsActive { get; set; }
    public int Version { get; set; }
    
    // Rotation tracking
    public DateTime? LastRotatedAt { get; set; }
    public int RotationIntervalDays { get; set; } = 90;
    public bool AutoRotate { get; set; }
    
    // Usage tracking
    public long UsageCount { get; set; }
    public DateTime? LastAuditAt { get; set; }
}

public class EncryptedData
{
    [Key]
    public long Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string EntityId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;
    
    public int EncryptionKeyId { get; set; }
    
    [ForeignKey(nameof(EncryptionKeyId))]
    public EncryptionKey EncryptionKey { get; set; } = null!;
    
    [Required]
    public string EncryptedValue { get; set; } = string.Empty;
    
    [Required]
    public string InitializationVector { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Salt { get; set; }
    
    public DateTime EncryptedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    
    [MaxLength(450)]
    public string EncryptedBy { get; set; } = string.Empty;
    
    // Compliance tracking
    public bool IsPersonalData { get; set; }
    public bool IsFinancialData { get; set; }
    public bool IsComplianceData { get; set; }
    
    [MaxLength(100)]
    public string? DataClassification { get; set; }
}

// System Monitoring Models
public class SystemHealthCheck
{
    [Key]
    public long Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Component { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string CheckName { get; set; } = string.Empty;
    
    public HealthStatus Status { get; set; }
    
    [MaxLength(1000)]
    public string? StatusMessage { get; set; }
    
    public string? CheckData { get; set; } // JSON
    
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    
    // Performance metrics
    public double? ResponseTimeMs { get; set; }
    public long? MemoryUsageMB { get; set; }
    public double? CpuUsagePercent { get; set; }
    public long? ActiveConnections { get; set; }
    
    // Thresholds
    public bool IsWarning { get; set; }
    public bool IsCritical { get; set; }
    
    [MaxLength(500)]
    public string? WarningThreshold { get; set; }
    
    [MaxLength(500)]
    public string? CriticalThreshold { get; set; }
}

public class SecurityScan
{
    [Key]
    public long Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ScanType { get; set; } = string.Empty;
    
    public SecurityScanStatus Status { get; set; }
    
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    
    [MaxLength(450)]
    public string? InitiatedBy { get; set; }
    
    // Scan results
    public int VulnerabilitiesFound { get; set; }
    public int HighRiskIssues { get; set; }
    public int MediumRiskIssues { get; set; }
    public int LowRiskIssues { get; set; }
    
    public string? ScanResults { get; set; } // JSON
    public string? Recommendations { get; set; } // JSON
    
    [MaxLength(1000)]
    public string? Summary { get; set; }
    
    public bool RequiresAction { get; set; }
    public bool IsCompliant { get; set; }
    
    // Follow-up tracking
    public DateTime? ReviewedAt { get; set; }
    
    [MaxLength(450)]
    public string? ReviewedBy { get; set; }
    
    [MaxLength(1000)]
    public string? ReviewNotes { get; set; }
}

// Enums
public enum MfaMethod
{
    None = 0,
    Totp = 1,
    Sms = 2,
    Email = 3,
    BackupCode = 4
}

public enum MfaChallengeStatus
{
    Pending = 0,
    Sent = 1,
    Verified = 2,
    Failed = 3,
    Expired = 4,
    Cancelled = 5
}

public enum AuditOperation
{
    Create = 0,
    Read = 1,
    Update = 2,
    Delete = 3,
    Login = 4,
    Logout = 5,
    Export = 6,
    Import = 7,
    Approve = 8,
    Reject = 9
}

public enum AuditSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum AuditCategory
{
    Authentication = 0,
    Authorization = 1,
    DataAccess = 2,
    DataModification = 3,
    SystemConfiguration = 4,
    UserManagement = 5,
    ClientData = 6,
    FinancialData = 7,
    ComplianceData = 8,
    SecurityEvent = 9
}

public enum SecurityEventSeverity
{
    Info = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum SecurityEventCategory
{
    Authentication = 0,
    Authorization = 1,
    DataBreach = 2,
    SuspiciousActivity = 3,
    SystemIntrusion = 4,
    MalwareDetection = 5,
    PolicyViolation = 6,
    ComplianceViolation = 7,
    AccessControl = 8,
    DataProtection = 9
}

public enum EncryptionKeyStatus
{
    Active = 0,
    Inactive = 1,
    Expired = 2,
    Compromised = 3,
    PendingRotation = 4
}

public enum EncryptionKeyType
{
    DataEncryption = 0,
    TokenSigning = 1,
    ApiEncryption = 2,
    DatabaseEncryption = 3,
    FileEncryption = 4,
    BackupEncryption = 5
}

public enum HealthStatus
{
    Healthy = 0,
    Warning = 1,
    Critical = 2,
    Unknown = 3
}

public enum SecurityScanStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}