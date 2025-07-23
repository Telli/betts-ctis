using BettsTax.Core.Services;
using BettsTax.Data;
using System.Security.Claims;
using System.Text.Json;

namespace BettsTax.Web.Middleware
{
    public class ClientPortalAuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ClientPortalAuditMiddleware> _logger;

        public ClientPortalAuditMiddleware(RequestDelegate next, ILogger<ClientPortalAuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuditService auditService, IUserContextService userContextService)
        {
            var originalBodyStream = context.Response.Body;
            var requestPath = context.Request.Path.Value?.ToLower();
            var method = context.Request.Method;
            var ipAddress = GetClientIpAddress(context);
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            // Only audit client portal API calls
            if (!IsClientPortalApiCall(requestPath))
            {
                await _next(context);
                return;
            }

            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var startTime = DateTime.UtcNow;
            var responseBody = string.Empty;
            var statusCode = 200;

            try
            {
                // Capture response body for successful operations
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                await _next(context);

                statusCode = context.Response.StatusCode;
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);

                // Log successful client portal activities
                if (statusCode >= 200 && statusCode < 300 && !string.IsNullOrEmpty(userId))
                {
                    await LogClientPortalActivity(
                        auditService, 
                        userContextService,
                        userId, 
                        method, 
                        requestPath, 
                        ipAddress, 
                        userAgent,
                        true,
                        null);
                }
            }
            catch (Exception ex)
            {
                statusCode = 500;
                
                // Log failed activities
                if (!string.IsNullOrEmpty(userId))
                {
                    await LogClientPortalActivity(
                        auditService, 
                        userContextService,
                        userId, 
                        method, 
                        requestPath, 
                        ipAddress, 
                        userAgent,
                        false,
                        ex.Message);
                }

                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
                
                // Log request timing for performance monitoring
                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation(
                    "Client Portal API: {Method} {Path} returned {StatusCode} in {Duration}ms for User {UserId} from IP {IPAddress}",
                    method, requestPath, statusCode, duration.TotalMilliseconds, userId, ipAddress);
            }
        }

        private static bool IsClientPortalApiCall(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            return path.StartsWith("/api/client-portal/", StringComparison.OrdinalIgnoreCase);
        }

        private async Task LogClientPortalActivity(
            IAuditService auditService,
            IUserContextService userContextService,
            string userId,
            string method,
            string? requestPath,
            string? ipAddress,
            string? userAgent,
            bool isSuccess,
            string? errorMessage)
        {
            try
            {
                var clientId = await userContextService.GetCurrentUserClientIdAsync();
                var actionType = GetActionTypeFromRequest(method, requestPath);
                var (entity, entityId) = ExtractEntityFromPath(requestPath);

                await auditService.LogClientPortalActivityAsync(
                    userId,
                    clientId,
                    actionType,
                    entity,
                    entityId,
                    $"{method} {requestPath}",
                    ipAddress,
                    userAgent,
                    requestPath,
                    isSuccess,
                    errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log client portal audit activity for user {UserId}", userId);
            }
        }

        private static AuditActionType GetActionTypeFromRequest(string method, string? path)
        {
            return method.ToUpper() switch
            {
                "GET" when path?.Contains("/download") == true => AuditActionType.Download,
                "GET" when path?.Contains("/documents") == true => AuditActionType.DocumentAccess,
                "GET" when path?.Contains("/compliance") == true => AuditActionType.ComplianceView,
                "GET" => AuditActionType.Read,
                "POST" when path?.Contains("/upload") == true => AuditActionType.Upload,
                "POST" when path?.Contains("/payments") == true => AuditActionType.PaymentRequest,
                "POST" => AuditActionType.Create,
                "PUT" when path?.Contains("/profile") == true => AuditActionType.ProfileUpdate,
                "PUT" => AuditActionType.Update,
                "DELETE" => AuditActionType.Delete,
                _ => AuditActionType.Read
            };
        }

        private static (string entity, string entityId) ExtractEntityFromPath(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return ("Unknown", "N/A");

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (segments.Length >= 3 && segments[0] == "api" && segments[1] == "client-portal")
            {
                var entity = segments[2];
                var entityId = segments.Length > 3 && int.TryParse(segments[3], out _) ? segments[3] : "N/A";
                
                return (entity, entityId);
            }

            return ("ClientPortal", "N/A");
        }

        private static string? GetClientIpAddress(HttpContext context)
        {
            // Check for various headers that might contain the real IP
            var headers = new[]
            {
                "CF-Connecting-IP",     // Cloudflare
                "X-Forwarded-For",      // Standard forwarded header
                "X-Real-IP",            // Nginx
                "X-Client-IP",          // Custom
                "True-Client-IP"        // Akamai
            };

            foreach (var header in headers)
            {
                var value = context.Request.Headers[header].FirstOrDefault();
                if (!string.IsNullOrEmpty(value))
                {
                    // X-Forwarded-For can contain multiple IPs, take the first one
                    var ip = value.Split(',').FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(ip) && ip != "unknown")
                    {
                        return ip;
                    }
                }
            }

            // Fall back to connection remote IP
            return context.Connection.RemoteIpAddress?.ToString();
        }
    }

    // Extension method for registering the middleware
    public static class ClientPortalAuditMiddlewareExtensions
    {
        public static IApplicationBuilder UseClientPortalAudit(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ClientPortalAuditMiddleware>();
        }
    }
}