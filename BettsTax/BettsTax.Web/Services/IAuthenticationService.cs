using BettsTax.Web.Models;

namespace BettsTax.Web.Services
{
    /// <summary>
    /// Authentication service interface
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Authenticate user with email and password
        /// </summary>
        Task<LoginResponse> AuthenticateAsync(LoginRequest request);

        /// <summary>
        /// Validate JWT token
        /// </summary>
        Task<bool> ValidateTokenAsync(string token);

        /// <summary>
        /// Get user from token
        /// </summary>
        Task<UserInfo?> GetUserFromTokenAsync(string token);

        /// <summary>
        /// Refresh access token
        /// </summary>
        Task<LoginResponse> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Revoke refresh token to terminate session
        /// </summary>
        Task RevokeRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Register a new user
        /// </summary>
        Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    }
}
