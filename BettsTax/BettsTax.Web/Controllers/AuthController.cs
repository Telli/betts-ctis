using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BettsTax.Web.Models;
using BettsTax.Web.Services;

namespace BettsTax.Web.Controllers
{
    /// <summary>
    /// Authentication Controller - Handles user authentication and token management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthenticationService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Login endpoint - authenticates user and returns JWT token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>Login response with JWT token</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Validate request
                if (!ModelState.IsValid)
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid request data",
                    });
                }

                _logger.LogInformation("Login attempt for email: {Email}", request.Email);

                // Authenticate user
                var response = await _authService.AuthenticateAsync(request);

                if (!response.Success)
                {
                    return Unauthorized(response);
                }

                _logger.LogInformation("Successful login for email: {Email}", request.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred during login"
                });
            }
        }

        /// <summary>
        /// Validate token endpoint - checks if a JWT token is valid
        /// </summary>
        /// <param name="token">JWT token to validate</param>
        /// <returns>Validation result</returns>
        [HttpPost("validate")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ValidateToken([FromBody] string token)
        {
            try
            {
                var isValid = await _authService.ValidateTokenAsync(token);

                if (!isValid)
                {
                    return Unauthorized(new { success = false, message = "Invalid token" });
                }

                return Ok(new { success = true, message = "Token is valid" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return Unauthorized(new { success = false, message = "Token validation failed" });
            }
        }

        /// <summary>
        /// Get current user information from token
        /// </summary>
        /// <returns>Current user information</returns>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { success = false, message = "No token provided" });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                var user = await _authService.GetUserFromTokenAsync(token);

                if (user == null)
                {
                    return Unauthorized(new { success = false, message = "Invalid token" });
                }

                return Ok(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return Unauthorized(new { success = false, message = "Error retrieving user information" });
            }
        }

        /// <summary>
        /// Logout endpoint - invalidates token (client-side token removal)
        /// </summary>
        /// <returns>Logout confirmation</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Logout()
        {
            // In a more sophisticated implementation, you would:
            // 1. Blacklist the token in Redis or database
            // 2. Invalidate any refresh tokens
            // 3. Log the logout event

            _logger.LogInformation("User logged out");
            return Ok(new { success = true, message = "Logged out successfully" });
        }

        /// <summary>
        /// Demo credentials endpoint - returns available demo credentials
        /// REMOVE THIS IN PRODUCTION
        /// </summary>
        /// <returns>List of demo credentials</returns>
        [HttpGet("demo-credentials")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetDemoCredentials()
        {
            var credentials = new[]
            {
                new { email = "staff@bettsfirm.com", password = "password", role = "Staff", description = "Staff member with access to all clients" },
                new { email = "client@example.com", password = "password", role = "Client", description = "Client user (ABC Corporation)" },
                new { email = "john@xyztrad.com", password = "password", role = "Client", description = "Client user (XYZ Trading)" },
                new { email = "admin@bettsfirm.com", password = "password", role = "Admin", description = "Administrator with full access" }
            };

            return Ok(new { success = true, data = credentials });
        }
    }
}
