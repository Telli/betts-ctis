using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using BettsTax.Data;
using BettsTax.Data.Models.Security;
using AuditLog = BettsTax.Data.Models.Security.AuditLog;
using BettsTax.Core.DTOs.Security;
using BettsTax.Core.Services.Interfaces;

namespace BettsTax.Core.Services.Security;

/// <summary>
/// Comprehensive audit service for tracking all system activities
/// Provides detailed audit trails for compliance and security monitoring
/// Implements Sierra Leone regulatory requirements for financial system auditing
/// </summary>
public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Sensitive fields that should be masked in audit logs
    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passwordhash", "token", "secret", "key", "pin", "ssn", "taxid",
        "accountnumber", "routingnumber", "creditcard", "cvv", "expiry"
    };

    public AuditService(
        ApplicationDbContext context,
        ILogger<AuditService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    #region Audit Logging

    public async Task LogAsync(string? userId, string action, string entity, string? entityId,
        AuditOperation operation, string? oldValues, string? newValues, string? description,
        AuditSeverity severity, AuditCategory category)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = GetClientIpAddress(httpContext);
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();
            var sessionId = httpContext?.Session?.Id;
            var requestId = httpContext?.TraceIdentifier;

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                Entity = entity,
                EntityId = entityId,
                Operation = operation,
                OldValues = MaskSensitiveData(oldValues),
                NewValues = MaskSensitiveData(newValues),
                Changes = GenerateChanges(oldValues, newValues),
                Description = description,
                Severity = severity,
                Category = category,
                IpAddress = ipAddress ?? "Unknown",
                UserAgent = userAgent,
                SessionId = sessionId,
                RequestId = requestId,
                Timestamp = DateTime.UtcNow,
                IsComplianceRelevant = DetermineComplianceRelevance(category, severity),
                IsSecurityEvent = DetermineSecurityRelevance(category, action),
                RequiresReview = DetermineReviewRequirement(severity, category),
                ComplianceTag = GenerateComplianceTag(category, entity)
            };

            _context.AuditLogs.Add(auditLog);

            // For high-severity events, also create a security event
            if (severity >= AuditSeverity.High || auditLog.IsSecurityEvent)
            {
                await CreateSecurityEventFromAudit(auditLog);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Audit log created: {Action} on {Entity} by user {UserId}", 
                action, entity, userId ?? "System");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for action {Action} on entity {Entity}", 
                action, entity);
            
            // Don't throw - audit failures shouldn't break business operations
            // But log to a separate audit failure mechanism if available
        }
    }

    public async Task LogLoginAttemptAsync(string? userId, string? username, bool success, 
        string? failureReason, string ipAddress, string? userAgent)
    {
        var action = success ? "LOGIN_SUCCESS" : "LOGIN_FAILED";
        var description = success 
            ? $"User {username} logged in successfully"
            : $"Login failed for user {username}. Reason: {failureReason}";

        await LogAsync(userId, action, "Authentication", userId, AuditOperation.Login,
            null, JsonSerializer.Serialize(new { Username = username, Success = success, FailureReason = failureReason }),
            description, success ? AuditSeverity.Low : AuditSeverity.Medium, AuditCategory.Authentication);

        // Log security event for failed login
        if (!success)
        {
            await LogSecurityEventAsync(userId, "FAILED_LOGIN", SecurityEventSeverity.Medium,
                SecurityEventCategory.Authentication, $"Failed login attempt for {username}",
                $"Login failed from IP {ipAddress}. Reason: {failureReason}",
                JsonSerializer.Serialize(new { Username = username, IpAddress = ipAddress, UserAgent = userAgent }));
        }
    }

    public async Task LogDataAccessAsync(string userId, string entity, string entityId, 
        string action, Dictionary<string, object>? additionalData = null)
    {
        var data = additionalData ?? new Dictionary<string, object>();
        data["AccessTime"] = DateTime.UtcNow;
        data["Entity"] = entity;
        data["EntityId"] = entityId;

        await LogAsync(userId, action, entity, entityId, AuditOperation.Read,
            null, JsonSerializer.Serialize(data),
            $"User accessed {entity} data",
            AuditSeverity.Low, AuditCategory.DataAccess);
    }

    public async Task LogDataModificationAsync(string userId, string entity, string entityId,
        object? oldData, object? newData, string operation)
    {
        var oldJson = oldData != null ? JsonSerializer.Serialize(oldData) : null;
        var newJson = newData != null ? JsonSerializer.Serialize(newData) : null;

        var auditOperation = operation.ToUpper() switch
        {
            "CREATE" => AuditOperation.Create,
            "UPDATE" => AuditOperation.Update,
            "DELETE" => AuditOperation.Delete,
            _ => AuditOperation.Update
        };

        await LogAsync(userId, $"{operation.ToUpper()}_{entity.ToUpper()}", entity, entityId,
            auditOperation, oldJson, newJson,
            $"User {operation.ToLower()}d {entity} record",
            AuditSeverity.Medium, AuditCategory.DataModification);
    }

    public async Task LogSecurityEventAsync(string? userId, string eventType, SecurityEventSeverity severity,
        SecurityEventCategory category, string title, string? description, string? eventData)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = GetClientIpAddress(httpContext);
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();
            var sessionId = httpContext?.Session?.Id;

            var securityEvent = new SecurityEvent
            {
                UserId = userId,
                EventType = eventType,
                Severity = severity,
                Category = category,
                Title = title,
                Description = description,
                EventData = eventData,
                IpAddress = ipAddress ?? "Unknown",
                UserAgent = userAgent,
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow,
                RiskScore = CalculateRiskScore(severity, category, eventType),
                RequiresInvestigation = severity >= SecurityEventSeverity.High ||
                                      category == SecurityEventCategory.DataBreach ||
                                      category == SecurityEventCategory.SystemIntrusion
            };

            _context.SecurityEvents.Add(securityEvent);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Security event logged: {EventType} - {Title} for user {UserId}", 
                eventType, title, userId ?? "Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event: {EventType}", eventType);
        }
    }

    #endregion

    #region Audit Queries

    public async Task<(List<AuditLogDto> Records, int TotalCount)> SearchAuditLogsAsync(AuditSearchCriteriaDto criteria)
    {
        try
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(criteria.UserId))
                query = query.Where(a => a.UserId == criteria.UserId);

            if (!string.IsNullOrEmpty(criteria.Action))
                query = query.Where(a => a.Action.Contains(criteria.Action));

            if (!string.IsNullOrEmpty(criteria.Entity))
                query = query.Where(a => a.Entity.Contains(criteria.Entity));

            if (criteria.Operation.HasValue)
                query = query.Where(a => a.Operation == criteria.Operation.Value);

            if (criteria.MinSeverity.HasValue)
                query = query.Where(a => a.Severity >= criteria.MinSeverity.Value);

            if (criteria.Category.HasValue)
                query = query.Where(a => a.Category == criteria.Category.Value);

            if (criteria.FromDate.HasValue)
                query = query.Where(a => a.Timestamp >= criteria.FromDate.Value);

            if (criteria.ToDate.HasValue)
                query = query.Where(a => a.Timestamp <= criteria.ToDate.Value);

            if (!string.IsNullOrEmpty(criteria.IpAddress))
                query = query.Where(a => a.IpAddress.Contains(criteria.IpAddress));

            if (criteria.ComplianceRelevantOnly == true)
                query = query.Where(a => a.IsComplianceRelevant);

            if (criteria.SecurityEventsOnly == true)
                query = query.Where(a => a.IsSecurityEvent);

            var totalCount = await query.CountAsync();

            var records = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((criteria.Page - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserName = a.User != null ? a.User.UserName : null,
                    Action = a.Action,
                    Entity = a.Entity,
                    EntityId = a.EntityId,
                    Operation = a.Operation,
                    OperationName = a.Operation.ToString(),
                    OldValues = a.OldValues,
                    NewValues = a.NewValues,
                    Changes = a.Changes,
                    Description = a.Description,
                    Severity = a.Severity,
                    SeverityName = a.Severity.ToString(),
                    Category = a.Category,
                    CategoryName = a.Category.ToString(),
                    IpAddress = a.IpAddress,
                    UserAgent = a.UserAgent,
                    SessionId = a.SessionId,
                    Timestamp = a.Timestamp,
                    IsComplianceRelevant = a.IsComplianceRelevant,
                    IsSecurityEvent = a.IsSecurityEvent,
                    ComplianceTag = a.ComplianceTag
                })
                .ToListAsync();

            _logger.LogInformation("Audit log search completed. Found {TotalCount} records", totalCount);
            return (records, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search audit logs");
            throw new InvalidOperationException("Audit log search failed", ex);
        }
    }

    public async Task<AuditReportDto> GenerateAuditReportAsync(AuditSearchCriteriaDto criteria)
    {
        try
        {
            var (records, totalCount) = await SearchAuditLogsAsync(criteria);

            var report = new AuditReportDto
            {
                GeneratedAt = DateTime.UtcNow,
                Criteria = criteria,
                TotalRecords = totalCount,
                ComplianceRelevantRecords = records.Count(r => r.IsComplianceRelevant),
                SecurityEventRecords = records.Count(r => r.IsSecurityEvent),
                Records = records
            };

            // Generate breakdowns
            report.ActionBreakdown = records
                .GroupBy(r => r.Action)
                .ToDictionary(g => g.Key, g => g.Count());

            report.SeverityBreakdown = records
                .GroupBy(r => r.SeverityName)
                .ToDictionary(g => g.Key, g => g.Count());

            report.CategoryBreakdown = records
                .GroupBy(r => r.CategoryName)
                .ToDictionary(g => g.Key, g => g.Count());

            _logger.LogInformation("Audit report generated with {RecordCount} records", records.Count);
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate audit report");
            throw new InvalidOperationException("Audit report generation failed", ex);
        }
    }

    public async Task<List<SecurityEventDto>> GetSecurityEventsAsync(SecurityEventSearchDto criteria)
    {
        try
        {
            var query = _context.SecurityEvents
                .Include(s => s.User)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(criteria.EventType))
                query = query.Where(s => s.EventType.Contains(criteria.EventType));

            if (criteria.MinSeverity.HasValue)
                query = query.Where(s => s.Severity >= criteria.MinSeverity.Value);

            if (criteria.Category.HasValue)
                query = query.Where(s => s.Category == criteria.Category.Value);

            if (criteria.FromDate.HasValue)
                query = query.Where(s => s.Timestamp >= criteria.FromDate.Value);

            if (criteria.ToDate.HasValue)
                query = query.Where(s => s.Timestamp <= criteria.ToDate.Value);

            if (!string.IsNullOrEmpty(criteria.UserId))
                query = query.Where(s => s.UserId == criteria.UserId);

            if (!string.IsNullOrEmpty(criteria.IpAddress))
                query = query.Where(s => s.IpAddress.Contains(criteria.IpAddress));

            if (criteria.UnresolvedOnly == true)
                query = query.Where(s => !s.IsResolved);

            if (criteria.RequiresInvestigation == true)
                query = query.Where(s => s.RequiresInvestigation);

            if (criteria.MinRiskScore.HasValue)
                query = query.Where(s => s.RiskScore >= criteria.MinRiskScore.Value);

            var results = await query
                .OrderByDescending(s => s.Timestamp)
                .Skip((criteria.Page - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .Select(s => new SecurityEventDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    UserName = s.User != null ? s.User.UserName : null,
                    EventType = s.EventType,
                    Severity = s.Severity,
                    SeverityName = s.Severity.ToString(),
                    Category = s.Category,
                    CategoryName = s.Category.ToString(),
                    Title = s.Title,
                    Description = s.Description,
                    EventData = s.EventData,
                    IpAddress = s.IpAddress,
                    UserAgent = s.UserAgent,
                    Timestamp = s.Timestamp,
                    RiskScore = s.RiskScore,
                    IsBlocked = s.IsBlocked,
                    IsResolved = s.IsResolved,
                    ResolvedBy = s.ResolvedBy,
                    ResolvedAt = s.ResolvedAt,
                    ResolutionNotes = s.ResolutionNotes,
                    RequiresInvestigation = s.RequiresInvestigation
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} security events", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security events");
            throw new InvalidOperationException("Security events retrieval failed", ex);
        }
    }

    public async Task<bool> ResolveSecurityEventAsync(SecurityEventResolutionDto resolution, string resolvedBy)
    {
        try
        {
            var securityEvent = await _context.SecurityEvents
                .FirstOrDefaultAsync(s => s.Id == resolution.EventId);

            if (securityEvent == null)
                return false;

            var oldStatus = JsonSerializer.Serialize(new { securityEvent.IsResolved, securityEvent.ResolvedBy, securityEvent.ResolvedAt });

            securityEvent.IsResolved = true;
            securityEvent.ResolvedBy = resolvedBy;
            securityEvent.ResolvedAt = DateTime.UtcNow;
            securityEvent.ResolutionNotes = resolution.ResolutionNotes;

            await _context.SaveChangesAsync();

            // Log the resolution
            await LogAsync(resolvedBy, "SECURITY_EVENT_RESOLVED", "SecurityEvent", resolution.EventId.ToString(),
                AuditOperation.Update, oldStatus, JsonSerializer.Serialize(securityEvent),
                $"Security event resolved: {securityEvent.Title}",
                AuditSeverity.Medium, AuditCategory.SecurityEvent);

            _logger.LogInformation("Security event {EventId} resolved by {ResolvedBy}", 
                resolution.EventId, resolvedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve security event {EventId}", resolution.EventId);
            return false;
        }
    }

    #endregion

    #region Compatibility Methods for IAuditService Interface

    public async Task LogAsync(string userId, string action, string entity, string entityId, string? details = null)
    {
        // Map old interface to new comprehensive audit logging
        await LogAsync(userId, action, entity, entityId, AuditOperation.Update, null, details, 
            details, AuditSeverity.Medium, AuditCategory.DataAccess);
    }

    public async Task LogClientPortalActivityAsync(
        string userId,
        int? clientId,
        AuditActionType actionType,
        string entity,
        string entityId,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? requestPath = null,
        bool isSuccess = true,
        string? errorMessage = null)
    {
        // Map old interface to new comprehensive audit logging
        var operation = actionType switch
        {
            AuditActionType.Create => AuditOperation.Create,
            AuditActionType.Read => AuditOperation.Read,
            AuditActionType.Update => AuditOperation.Update,
            AuditActionType.Delete => AuditOperation.Delete,
            AuditActionType.Login => AuditOperation.Login,
            _ => AuditOperation.Update
        };

        var severity = isSuccess ? AuditSeverity.Low : AuditSeverity.Medium;
        var description = $"Client Portal Activity: {actionType} on {entity} for client {clientId}";
        
        if (!isSuccess && !string.IsNullOrEmpty(errorMessage))
        {
            description += $". Error: {errorMessage}";
        }

        await LogAsync(userId, actionType.ToString(), entity, entityId, operation, 
            null, details, description, severity, AuditCategory.ClientData);
    }

    public async Task LogSecurityEventAsync(
        string? userId,
        AuditActionType actionType,
        string details,
        string? ipAddress = null,
        string? userAgent = null,
        bool isSuccess = false,
        string? errorMessage = null)
    {
        var severity = isSuccess ? SecurityEventSeverity.Low : SecurityEventSeverity.Medium;
        var category = SecurityEventCategory.AccessControl;
        var title = $"Security Event: {actionType}";
        var description = details;
        
        if (!isSuccess && !string.IsNullOrEmpty(errorMessage))
        {
            description += $". Error: {errorMessage}";
            severity = SecurityEventSeverity.High;
        }

        await LogSecurityEventAsync(userId, actionType.ToString(), severity, category, 
            title, description, details);
    }

    #endregion

    #region Private Methods

    private string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null) return null;

        // Check for forwarded IP first (common in load balancer scenarios)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? MaskSensitiveData(string? jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
            return jsonData;

        try
        {
            var jsonDoc = JsonDocument.Parse(jsonData);
            var maskedData = MaskJsonElement(jsonDoc.RootElement);
            return JsonSerializer.Serialize(maskedData, new JsonSerializerOptions { WriteIndented = false });
        }
        catch
        {
            // If JSON parsing fails, return as-is
            return jsonData;
        }
    }

    private object MaskJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    var value = SensitiveFields.Contains(property.Name) 
                        ? "***MASKED***" 
                        : MaskJsonElement(property.Value);
                    obj[property.Name] = value;
                }
                return obj;

            case JsonValueKind.Array:
                return element.EnumerateArray().Select(MaskJsonElement).ToArray();

            case JsonValueKind.String:
                return element.GetString() ?? "";

            case JsonValueKind.Number:
                return element.TryGetInt64(out var longValue) ? longValue : element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null;

            default:
                return element.ToString();
        }
    }

    private string? GenerateChanges(string? oldValues, string? newValues)
    {
        if (string.IsNullOrEmpty(oldValues) || string.IsNullOrEmpty(newValues))
            return null;

        try
        {
            var oldDoc = JsonDocument.Parse(oldValues);
            var newDoc = JsonDocument.Parse(newValues);
            
            var changes = new Dictionary<string, object>();
            CompareJsonElements(oldDoc.RootElement, newDoc.RootElement, "", changes);
            
            return changes.Count > 0 ? JsonSerializer.Serialize(changes) : null;
        }
        catch
        {
            return null;
        }
    }

    private void CompareJsonElements(JsonElement oldElement, JsonElement newElement, string path, 
        Dictionary<string, object> changes)
    {
        if (oldElement.ValueKind != newElement.ValueKind)
        {
            changes[path] = new { Old = oldElement.ToString(), New = newElement.ToString() };
            return;
        }

        switch (oldElement.ValueKind)
        {
            case JsonValueKind.Object:
                var oldProperties = oldElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                var newProperties = newElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

                foreach (var prop in oldProperties.Keys.Union(newProperties.Keys))
                {
                    var propPath = string.IsNullOrEmpty(path) ? prop : $"{path}.{prop}";
                    
                    if (!oldProperties.ContainsKey(prop))
                    {
                        changes[propPath] = new { Old = (object?)null, New = newProperties[prop].ToString() };
                    }
                    else if (!newProperties.ContainsKey(prop))
                    {
                        changes[propPath] = new { Old = oldProperties[prop].ToString(), New = (object?)null };
                    }
                    else
                    {
                        CompareJsonElements(oldProperties[prop], newProperties[prop], propPath, changes);
                    }
                }
                break;

            case JsonValueKind.Array:
                var oldArray = oldElement.EnumerateArray().ToArray();
                var newArray = newElement.EnumerateArray().ToArray();
                
                if (oldArray.Length != newArray.Length)
                {
                    changes[path] = new { Old = $"Array[{oldArray.Length}]", New = $"Array[{newArray.Length}]" };
                    return;
                }

                for (int i = 0; i < oldArray.Length; i++)
                {
                    CompareJsonElements(oldArray[i], newArray[i], $"{path}[{i}]", changes);
                }
                break;

            default:
                if (oldElement.ToString() != newElement.ToString())
                {
                    changes[path] = new { Old = oldElement.ToString(), New = newElement.ToString() };
                }
                break;
        }
    }

    private bool DetermineComplianceRelevance(AuditCategory category, AuditSeverity severity)
    {
        return category switch
        {
            AuditCategory.FinancialData => true,
            AuditCategory.ClientData => true,
            AuditCategory.ComplianceData => true,
            AuditCategory.UserManagement when severity >= AuditSeverity.Medium => true,
            AuditCategory.SystemConfiguration when severity >= AuditSeverity.Medium => true,
            AuditCategory.SecurityEvent => true,
            _ => severity >= AuditSeverity.High
        };
    }

    private bool DetermineSecurityRelevance(AuditCategory category, string action)
    {
        var securityActions = new[] { "LOGIN_FAILED", "MFA_FAILED", "UNAUTHORIZED_ACCESS", "PERMISSION_DENIED" };
        
        return category == AuditCategory.SecurityEvent ||
               category == AuditCategory.Authentication ||
               securityActions.Any(sa => action.Contains(sa, StringComparison.OrdinalIgnoreCase));
    }

    private bool DetermineReviewRequirement(AuditSeverity severity, AuditCategory category)
    {
        return severity >= AuditSeverity.High ||
               category == AuditCategory.FinancialData ||
               category == AuditCategory.ComplianceData ||
               category == AuditCategory.SecurityEvent;
    }

    private string? GenerateComplianceTag(AuditCategory category, string entity)
    {
        return category switch
        {
            AuditCategory.FinancialData => "FINANCE_ACT_2025",
            AuditCategory.ClientData => "DATA_PROTECTION",
            AuditCategory.ComplianceData => "TAX_COMPLIANCE",
            AuditCategory.Authentication => "ACCESS_CONTROL",
            AuditCategory.UserManagement => "USER_GOVERNANCE",
            _ => null
        };
    }

    private async Task CreateSecurityEventFromAudit(BettsTax.Data.Models.Security.AuditLog auditLog)
    {
        try
        {
            var securityEvent = new SecurityEvent
            {
                UserId = auditLog.UserId,
                EventType = auditLog.Action,
                Severity = auditLog.Severity switch
                {
                    AuditSeverity.Low => SecurityEventSeverity.Low,
                    AuditSeverity.Medium => SecurityEventSeverity.Medium,
                    AuditSeverity.High => SecurityEventSeverity.High,
                    AuditSeverity.Critical => SecurityEventSeverity.Critical,
                    _ => SecurityEventSeverity.Medium
                },
                Category = SecurityEventCategory.AccessControl,
                Title = $"High-severity audit event: {auditLog.Action}",
                Description = auditLog.Description,
                EventData = JsonSerializer.Serialize(new
                {
                    AuditLogId = auditLog.Id,
                    Entity = auditLog.Entity,
                    EntityId = auditLog.EntityId,
                    Operation = auditLog.Operation.ToString()
                }),
                IpAddress = auditLog.IpAddress,
                UserAgent = auditLog.UserAgent,
                SessionId = auditLog.SessionId,
                Timestamp = auditLog.Timestamp,
                RiskScore = CalculateRiskScore(
                    auditLog.Severity switch
                    {
                        AuditSeverity.High => SecurityEventSeverity.High,
                        AuditSeverity.Critical => SecurityEventSeverity.Critical,
                        _ => SecurityEventSeverity.Medium
                    }, 
                    SecurityEventCategory.AccessControl, 
                    auditLog.Action)
            };

            _context.SecurityEvents.Add(securityEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create security event from audit log {AuditLogId}", auditLog.Id);
        }
    }

    private int CalculateRiskScore(SecurityEventSeverity severity, SecurityEventCategory category, string eventType)
    {
        var baseScore = severity switch
        {
            SecurityEventSeverity.Low => 20,
            SecurityEventSeverity.Medium => 40,
            SecurityEventSeverity.High => 70,
            SecurityEventSeverity.Critical => 90,
            _ => 10
        };

        var categoryMultiplier = category switch
        {
            SecurityEventCategory.DataBreach => 1.5,
            SecurityEventCategory.SystemIntrusion => 1.4,
            SecurityEventCategory.Authentication => 1.2,
            SecurityEventCategory.Authorization => 1.2,
            SecurityEventCategory.ComplianceViolation => 1.3,
            _ => 1.0
        };

        var eventTypeBonus = eventType.ToUpper() switch
        {
            var type when type.Contains("FAILED_LOGIN") => 5,
            var type when type.Contains("MFA") => 10,
            var type when type.Contains("UNAUTHORIZED") => 15,
            var type when type.Contains("BREACH") => 20,
            _ => 0
        };

        return Math.Min(100, (int)(baseScore * categoryMultiplier) + eventTypeBonus);
    }

    #endregion
}