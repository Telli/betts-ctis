using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Core.DTOs.Security;
using BettsTax.Data.Models.Security;

namespace BettsTax.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SecurityController : ControllerBase
{
    private readonly IMfaService _mfaService;
    private readonly IAuditService _auditService;
    private readonly IEncryptionService _encryptionService;
    private readonly ISystemMonitoringService _monitoringService;
    private readonly IUserSecurityService _userSecurityService;
    private readonly IComplianceMonitoringService _complianceService;
    private readonly ILogger<SecurityController> _logger;

    public SecurityController(
        IMfaService mfaService,
        IAuditService auditService,
        IEncryptionService encryptionService,
        ISystemMonitoringService monitoringService,
        IUserSecurityService userSecurityService,
        IComplianceMonitoringService complianceService,
        ILogger<SecurityController> logger)
    {
        _mfaService = mfaService;
        _auditService = auditService;
        _encryptionService = encryptionService;
        _monitoringService = monitoringService;
        _userSecurityService = userSecurityService;
        _complianceService = complianceService;
        _logger = logger;
    }

    #region Multi-Factor Authentication

    /// <summary>
    /// Setup Multi-Factor Authentication for current user
    /// </summary>
    [HttpPost("mfa/setup")]
    public async Task<ActionResult<MfaSetupResultDto>> SetupMfa([FromBody] MfaSetupDto setupDto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var result = await _mfaService.SetupMfaAsync(userId, setupDto);

            if (!result.Success)
                return BadRequest(result.Message);

            _logger.LogInformation("MFA setup completed for user {UserId}", userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup MFA");
            return StatusCode(500, "MFA setup failed");
        }
    }

    /// <summary>
    /// Get current MFA configuration
    /// </summary>
    [HttpGet("mfa/configuration")]
    public async Task<ActionResult<MfaSetupDto>> GetMfaConfiguration()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var config = await _mfaService.GetMfaConfigurationAsync(userId);

            if (config == null)
                return NotFound("MFA not configured");

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get MFA configuration");
            return StatusCode(500, "Failed to retrieve MFA configuration");
        }
    }

    /// <summary>
    /// Create MFA challenge
    /// </summary>
    [HttpPost("mfa/challenge")]
    [AllowAnonymous]
    public async Task<ActionResult<MfaChallengeDto>> CreateMfaChallenge([FromBody] MfaChallengeRequestDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not authenticated");

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var challenge = await _mfaService.CreateChallengeAsync(userId, request, ipAddress, userAgent);

            if (challenge == null)
                return BadRequest("Failed to create MFA challenge");

            return Ok(challenge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MFA challenge");
            return StatusCode(500, "MFA challenge creation failed");
        }
    }

    /// <summary>
    /// Verify MFA challenge
    /// </summary>
    [HttpPost("mfa/verify")]
    [AllowAnonymous]
    public async Task<ActionResult<MfaVerificationResultDto>> VerifyMfaChallenge([FromBody] MfaVerificationDto verification)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not authenticated");

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var result = await _mfaService.VerifyChallengeAsync(userId, verification, ipAddress, userAgent);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify MFA challenge");
            return StatusCode(500, "MFA verification failed");
        }
    }

    /// <summary>
    /// Disable MFA (Admin only)
    /// </summary>
    [HttpPost("mfa/disable/{userId}")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult> DisableMfa(string userId)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var success = await _mfaService.DisableMfaAsync(userId, currentUserId);

            if (!success)
                return NotFound("User not found or MFA not configured");

            _logger.LogWarning("MFA disabled for user {UserId} by {AdminUserId}", userId, currentUserId);
            return Ok("MFA disabled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable MFA for user {UserId}", userId);
            return StatusCode(500, "Failed to disable MFA");
        }
    }

    #endregion

    #region Audit Logs

    /// <summary>
    /// Search audit logs
    /// </summary>
    [HttpPost("audit/search")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<List<AuditLogDto>>> SearchAuditLogs([FromBody] AuditSearchCriteriaDto criteria)
    {
        try
        {
            var (records, totalCount) = await _auditService.SearchAuditLogsAsync(criteria);

            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search audit logs");
            return StatusCode(500, "Audit log search failed");
        }
    }

    /// <summary>
    /// Generate audit report
    /// </summary>
    [HttpPost("audit/report")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<AuditReportDto>> GenerateAuditReport([FromBody] AuditSearchCriteriaDto criteria)
    {
        try
        {
            var report = await _auditService.GenerateAuditReportAsync(criteria);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate audit report");
            return StatusCode(500, "Audit report generation failed");
        }
    }

    /// <summary>
    /// Get security events
    /// </summary>
    [HttpPost("audit/security-events")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<List<SecurityEventDto>>> GetSecurityEvents([FromBody] SecurityEventSearchDto criteria)
    {
        try
        {
            var events = await _auditService.GetSecurityEventsAsync(criteria);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security events");
            return StatusCode(500, "Failed to retrieve security events");
        }
    }

    /// <summary>
    /// Resolve security event
    /// </summary>
    [HttpPost("audit/security-events/resolve")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult> ResolveSecurityEvent([FromBody] SecurityEventResolutionDto resolution)
    {
        try
        {
            var resolvedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var success = await _auditService.ResolveSecurityEventAsync(resolution, resolvedBy);

            if (!success)
                return NotFound("Security event not found");

            return Ok("Security event resolved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve security event {EventId}", resolution.EventId);
            return StatusCode(500, "Failed to resolve security event");
        }
    }

    #endregion

    #region Encryption Management

    /// <summary>
    /// Create encryption key
    /// </summary>
    [HttpPost("encryption/keys")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<EncryptionKeyDto>> CreateEncryptionKey([FromBody] CreateEncryptionKeyDto request)
    {
        try
        {
            var createdBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var key = await _encryptionService.CreateEncryptionKeyAsync(request, createdBy);

            return CreatedAtAction(nameof(GetEncryptionKeys), new { keyType = request.KeyType }, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create encryption key {KeyName}", request.KeyName);
            return StatusCode(500, "Encryption key creation failed");
        }
    }

    /// <summary>
    /// Get encryption keys
    /// </summary>
    [HttpGet("encryption/keys")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<List<EncryptionKeyDto>>> GetEncryptionKeys([FromQuery] EncryptionKeyType? keyType = null, [FromQuery] bool? activeOnly = true)
    {
        try
        {
            var keys = await _encryptionService.GetEncryptionKeysAsync(keyType, activeOnly);
            return Ok(keys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get encryption keys");
            return StatusCode(500, "Failed to retrieve encryption keys");
        }
    }

    /// <summary>
    /// Rotate encryption key
    /// </summary>
    [HttpPost("encryption/keys/{keyId}/rotate")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult> RotateEncryptionKey(int keyId)
    {
        try
        {
            var rotatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var success = await _encryptionService.RotateKeyAsync(keyId, rotatedBy);

            if (!success)
                return NotFound("Encryption key not found");

            return Ok("Encryption key rotated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate encryption key {KeyId}", keyId);
            return StatusCode(500, "Key rotation failed");
        }
    }

    /// <summary>
    /// Get encryption statistics
    /// </summary>
    [HttpGet("encryption/statistics")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<EncryptionStatisticsDto>> GetEncryptionStatistics()
    {
        try
        {
            var statistics = await _encryptionService.GetEncryptionStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get encryption statistics");
            return StatusCode(500, "Failed to retrieve encryption statistics");
        }
    }

    #endregion

    #region System Monitoring

    /// <summary>
    /// Get system health overview
    /// </summary>
    [HttpGet("monitoring/health")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<SystemHealthOverviewDto>> GetSystemHealth()
    {
        try
        {
            var health = await _monitoringService.GetSystemHealthAsync();
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system health");
            return StatusCode(500, "Failed to retrieve system health");
        }
    }

    /// <summary>
    /// Start security scan
    /// </summary>
    [HttpPost("monitoring/security-scan")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<SecurityScanDto>> StartSecurityScan([FromBody] StartSecurityScanDto request)
    {
        try
        {
            var initiatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var scan = await _monitoringService.StartSecurityScanAsync(request, initiatedBy);

            return Ok(scan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start security scan");
            return StatusCode(500, "Failed to start security scan");
        }
    }

    /// <summary>
    /// Get security dashboard
    /// </summary>
    [HttpGet("monitoring/dashboard")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<SecurityDashboardDto>> GetSecurityDashboard()
    {
        try
        {
            var dashboard = await _monitoringService.GetSecurityDashboardAsync();
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security dashboard");
            return StatusCode(500, "Failed to retrieve security dashboard");
        }
    }

    #endregion

    #region User Security Management

    /// <summary>
    /// Get user security profile
    /// </summary>
    [HttpGet("users/{userId}/security-profile")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<UserSecurityProfileDto>> GetUserSecurityProfile(string userId)
    {
        try
        {
            var profile = await _userSecurityService.GetUserSecurityProfileAsync(userId);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security profile for user {UserId}", userId);
            return StatusCode(500, "Failed to retrieve user security profile");
        }
    }

    /// <summary>
    /// Get users requiring security attention
    /// </summary>
    [HttpGet("users/requiring-attention")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<List<UserSecurityProfileDto>>> GetUsersRequiringAttention()
    {
        try
        {
            var users = await _userSecurityService.GetUsersRequiringAttentionAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users requiring attention");
            return StatusCode(500, "Failed to retrieve users requiring attention");
        }
    }

    /// <summary>
    /// Lock user account
    /// </summary>
    [HttpPost("users/{userId}/lock")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult> LockUserAccount(string userId, [FromBody] UserLockRequestDto request)
    {
        try
        {
            var lockedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var success = await _userSecurityService.LockUserAccountAsync(userId, request.Reason, lockedBy);

            if (!success)
                return NotFound("User not found");

            return Ok("User account locked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lock user account {UserId}", userId);
            return StatusCode(500, "Failed to lock user account");
        }
    }

    /// <summary>
    /// Unlock user account
    /// </summary>
    [HttpPost("users/{userId}/unlock")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult> UnlockUserAccount(string userId, [FromBody] UserUnlockRequestDto request)
    {
        try
        {
            var unlockedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var success = await _userSecurityService.UnlockUserAccountAsync(userId, request.Reason, unlockedBy);

            if (!success)
                return NotFound("User not found");

            return Ok("User account unlocked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unlock user account {UserId}", userId);
            return StatusCode(500, "Failed to unlock user account");
        }
    }

    /// <summary>
    /// Perform bulk security actions
    /// </summary>
    [HttpPost("users/bulk-action")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<List<string>>> BulkSecurityAction([FromBody] BulkSecurityActionDto request)
    {
        try
        {
            var performedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var results = await _userSecurityService.BulkSecurityActionAsync(request, performedBy);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform bulk security action");
            return StatusCode(500, "Bulk security action failed");
        }
    }

    #endregion

    #region Compliance Monitoring

    /// <summary>
    /// Get compliance status
    /// </summary>
    [HttpGet("compliance/status")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<ComplianceStatusDto>> GetComplianceStatus()
    {
        try
        {
            var status = await _complianceService.GetComplianceStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get compliance status");
            return StatusCode(500, "Failed to retrieve compliance status");
        }
    }

    /// <summary>
    /// Run compliance checks
    /// </summary>
    [HttpPost("compliance/run-checks")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<List<ComplianceCheckDto>>> RunComplianceChecks()
    {
        try
        {
            var checks = await _complianceService.RunComplianceChecksAsync();
            return Ok(checks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run compliance checks");
            return StatusCode(500, "Compliance checks failed");
        }
    }

    /// <summary>
    /// Get failed compliance checks
    /// </summary>
    [HttpGet("compliance/failed-checks")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<List<ComplianceCheckDto>>> GetFailedComplianceChecks()
    {
        try
        {
            var checks = await _complianceService.GetFailedComplianceChecksAsync();
            return Ok(checks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get failed compliance checks");
            return StatusCode(500, "Failed to retrieve failed compliance checks");
        }
    }

    #endregion

    #region Security Settings

    /// <summary>
    /// Get security settings
    /// </summary>
    [HttpGet("settings")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public ActionResult<SecuritySettingsDto> GetSecuritySettings()
    {
        var settings = new SecuritySettingsDto
        {
            MfaEnabled = true,
            MfaEnforced = false,
            PasswordPolicy = new PasswordPolicyDto
            {
                MinLength = 8,
                RequireDigits = true,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireNonAlphanumeric = true,
                RequiredUniqueChars = 6,
                MaxAgeInDays = 90
            },
            SessionPolicy = new SessionPolicyDto
            {
                TimeoutMinutes = 60,
                RequireHttps = true,
                SameSiteMode = "Strict",
                SecureCookies = true
            },
            AuditSettings = new AuditSettingsDto
            {
                RetentionDays = 2555, // 7 years for financial compliance
                LogLevel = "Information",
                EnableDataMasking = true,
                RealTimeAlerts = true
            }
        };

        return Ok(settings);
    }

    #endregion
}

// Supporting DTOs for API requests
public class UserLockRequestDto
{
    public string Reason { get; set; } = string.Empty;
}

public class UserUnlockRequestDto
{
    public string Reason { get; set; } = string.Empty;
}

public class SecuritySettingsDto
{
    public bool MfaEnabled { get; set; }
    public bool MfaEnforced { get; set; }
    public PasswordPolicyDto PasswordPolicy { get; set; } = new();
    public SessionPolicyDto SessionPolicy { get; set; } = new();
    public AuditSettingsDto AuditSettings { get; set; } = new();
}

public class PasswordPolicyDto
{
    public int MinLength { get; set; }
    public bool RequireDigits { get; set; }
    public bool RequireLowercase { get; set; }
    public bool RequireUppercase { get; set; }
    public bool RequireNonAlphanumeric { get; set; }
    public int RequiredUniqueChars { get; set; }
    public int MaxAgeInDays { get; set; }
}

public class SessionPolicyDto
{
    public int TimeoutMinutes { get; set; }
    public bool RequireHttps { get; set; }
    public string SameSiteMode { get; set; } = string.Empty;
    public bool SecureCookies { get; set; }
}

public class AuditSettingsDto
{
    public int RetentionDays { get; set; }
    public string LogLevel { get; set; } = string.Empty;
    public bool EnableDataMasking { get; set; }
    public bool RealTimeAlerts { get; set; }
}