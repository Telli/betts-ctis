using System.Security.Claims;

namespace BettsTax.Web.Services
{
    /// <summary>
    /// Authorization service interface for verifying user access to resources
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Check if the current user can access the specified client's data
        /// </summary>
        /// <param name="user">Current user claims principal</param>
        /// <param name="clientId">Client ID to access</param>
        /// <returns>True if authorized, false otherwise</returns>
        bool CanAccessClientData(ClaimsPrincipal user, int? clientId);

        /// <summary>
        /// Get the client ID associated with the current user (for client role)
        /// </summary>
        /// <param name="user">Current user claims principal</param>
        /// <returns>Client ID if user is a client, null otherwise</returns>
        int? GetUserClientId(ClaimsPrincipal user);

        /// <summary>
        /// Check if user has staff or admin role
        /// </summary>
        /// <param name="user">Current user claims principal</param>
        /// <returns>True if user is staff or admin</returns>
        bool IsStaffOrAdmin(ClaimsPrincipal user);

        /// <summary>
        /// Get user role from claims
        /// </summary>
        /// <param name="user">Current user claims principal</param>
        /// <returns>User role string</returns>
        string GetUserRole(ClaimsPrincipal user);
    }
}
