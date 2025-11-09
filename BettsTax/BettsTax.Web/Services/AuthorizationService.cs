using System.Security.Claims;

namespace BettsTax.Web.Services
{
    /// <summary>
    /// Authorization service implementation for access control
    /// </summary>
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ILogger<AuthorizationService> _logger;

        public AuthorizationService(ILogger<AuthorizationService> logger)
        {
            _logger = logger;
        }

        public bool CanAccessClientData(ClaimsPrincipal user, int? clientId)
        {
            if (user == null)
            {
                _logger.LogWarning("Authorization check failed: user is null");
                return false;
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = GetUserRole(user);

            // If no specific clientId is requested, allow access
            // (the service layer should handle filtering based on user permissions)
            if (!clientId.HasValue)
            {
                return true;
            }

            // Admin and Staff roles can access any client's data
            if (IsStaffOrAdmin(user))
            {
                _logger.LogDebug(
                    "User {UserId} with role {Role} granted access to ClientId {ClientId}",
                    userId, userRole, clientId);
                return true;
            }

            // Client users can only access their own data
            var userClientId = GetUserClientId(user);
            if (userClientId.HasValue && userClientId.Value == clientId.Value)
            {
                _logger.LogDebug(
                    "User {UserId} granted access to their own data (ClientId {ClientId})",
                    userId, clientId);
                return true;
            }

            // Access denied
            _logger.LogWarning(
                "Access denied: User {UserId} with role {Role} attempted to access ClientId {ClientId} " +
                "(User's ClientId: {UserClientId})",
                userId, userRole, clientId, userClientId?.ToString() ?? "null");

            return false;
        }

        public int? GetUserClientId(ClaimsPrincipal user)
        {
            if (user == null)
            {
                return null;
            }

            var clientIdClaim = user.FindFirstValue("ClientId");
            if (int.TryParse(clientIdClaim, out var clientId))
            {
                return clientId;
            }

            return null;
        }

        public bool IsStaffOrAdmin(ClaimsPrincipal user)
        {
            if (user == null)
            {
                return false;
            }

            var role = GetUserRole(user);
            return role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                   role.Equals("Staff", StringComparison.OrdinalIgnoreCase);
        }

        public string GetUserRole(ClaimsPrincipal user)
        {
            if (user == null)
            {
                return string.Empty;
            }

            return user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        }
    }
}
