using BettsTax.Core.DTOs.Auth;
using BettsTax.Core.Services;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Web.Constants;
using BettsTax.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtTokenGenerator _jwtGenerator;
        private readonly IActivityTimelineService _activityService;
        private readonly ApplicationDbContext _context;
        private readonly ISamlAuthenticationService _samlService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IAntiforgery _antiforgery;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            JwtTokenGenerator jwtGenerator,
            IActivityTimelineService activityService,
            ApplicationDbContext context,
            ISamlAuthenticationService samlService,
            IRefreshTokenService refreshTokenService,
            IAntiforgery antiforgery,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _jwtGenerator = jwtGenerator;
            _activityService = activityService;
            _context = context;
            _samlService = samlService;
            _refreshTokenService = refreshTokenService;
            _antiforgery = antiforgery;
            _logger = logger;
        }

        private static CookieOptions BuildHttpOnlyCookieOptions(DateTimeOffset expires, bool httpOnly = true)
        {
            return new CookieOptions
            {
                HttpOnly = httpOnly,
                SameSite = SameSiteMode.Strict,
                Secure = true,
                Expires = expires,
                Path = "/"
            };
        }

        private void AppendAccessTokenCookie(string token)
        {
            var expires = DateTimeOffset.UtcNow.AddMinutes(30);
            Response.Cookies.Append(
                AuthConstants.AccessTokenCookieName,
                token,
                BuildHttpOnlyCookieOptions(expires));
        }

        private void AppendRefreshTokenCookie(string token, DateTime expiresAt)
        {
            var expires = DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc);
            Response.Cookies.Append(
                AuthConstants.RefreshTokenCookieName,
                token,
                BuildHttpOnlyCookieOptions(expires, httpOnly: true));
        }

        private void ClearAuthCookies()
        {
            var expired = DateTimeOffset.UtcNow.AddDays(-1);
            Response.Cookies.Append(AuthConstants.AccessTokenCookieName, string.Empty, BuildHttpOnlyCookieOptions(expired));
            Response.Cookies.Append(AuthConstants.RefreshTokenCookieName, string.Empty, BuildHttpOnlyCookieOptions(expired));
        }

        private string GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        private string GetUserAgent() => Request.Headers["User-Agent"].ToString();

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email, FirstName = dto.FirstName, LastName = dto.LastName };
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Assign default role for development (Admin for now)
            await _userManager.AddToRoleAsync(user, "Admin");

            return Ok();
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", dto.Email);
                
                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null) 
                {
                    _logger.LogWarning("User not found for email: {Email}", dto.Email);
                    return Unauthorized();
                }
                
                _logger.LogInformation("User found, checking password for: {Email}", dto.Email);
                if (!await _userManager.CheckPasswordAsync(user, dto.Password)) 
                {
                    _logger.LogWarning("Invalid password for email: {Email}", dto.Email);
                    return Unauthorized();
                }
                
                _logger.LogInformation("Password valid, updating last login for: {Email}", dto.Email);
                // Update last login date
                user.LastLoginDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                
                _logger.LogInformation("Generating token for: {Email}", dto.Email);
                var roles = await _userManager.GetRolesAsync(user);
                var token = _jwtGenerator.GenerateToken(user.Id, user.Email!, roles);
                
                _logger.LogInformation("Token generated successfully for: {Email}", dto.Email);
                // Log activity for client users
                var primaryRole = roles.FirstOrDefault() ?? "Client";
                if (primaryRole == "Client")
                {
                    var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == user.Id);
                    if (client != null)
                    {
                        await _activityService.LogClientLoginAsync(client.ClientId);
                    }
                }
                
                var refreshToken = await _refreshTokenService.CreateAsync(user, GetClientIp(), GetUserAgent());
                AppendAccessTokenCookie(token);
                AppendRefreshTokenCookie(refreshToken.RawToken, refreshToken.Entity.ExpiresAt);

                _logger.LogInformation("Login successful for: {Email}", dto.Email);
                return Ok(new { token, roles });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for email: {Email}", dto.Email);
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken()
        {
            if (!Request.Cookies.TryGetValue(AuthConstants.RefreshTokenCookieName, out var rawRefreshToken) || string.IsNullOrWhiteSpace(rawRefreshToken))
            {
                return Unauthorized();
            }

            var existingToken = await _refreshTokenService.GetValidTokenAsync(rawRefreshToken);
            if (existingToken?.User == null)
            {
                ClearAuthCookies();
                return Unauthorized();
            }

            var roles = await _userManager.GetRolesAsync(existingToken.User);
            var jwt = _jwtGenerator.GenerateToken(existingToken.User.Id, existingToken.User.Email!, roles);

            var rotated = await _refreshTokenService.RotateAsync(existingToken, GetClientIp(), GetUserAgent());
            AppendAccessTokenCookie(jwt);
            AppendRefreshTokenCookie(rotated.RawToken, rotated.Entity.ExpiresAt);

            return Ok(new { token = jwt, roles });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                if (Request.Cookies.TryGetValue(AuthConstants.RefreshTokenCookieName, out var rawRefreshToken) && !string.IsNullOrWhiteSpace(rawRefreshToken))
                {
                    var existingToken = await _refreshTokenService.GetValidTokenAsync(rawRefreshToken);
                    if (existingToken != null)
                    {
                        await _refreshTokenService.RevokeAsync(existingToken, "User logout", GetClientIp(), GetUserAgent());
                    }
                }

                ClearAuthCookies();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed for user {UserId}", User?.Identity?.Name);
                return StatusCode(500, "Logout failed");
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            var activeTokens = await _context.RefreshTokens
                .Where(r => r.UserId == user.Id && !r.RevokedAt.HasValue && r.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                await _refreshTokenService.RevokeAsync(token, "Password changed", GetClientIp(), GetUserAgent(), compromised: false);
            }

            ClearAuthCookies();
            return Ok(new { message = "Password updated. Please log in again." });
        }

        [HttpGet("csrf-token")]
        [AllowAnonymous]
        public IActionResult GetCsrfToken()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            if (!string.IsNullOrWhiteSpace(tokens.RequestToken))
            {
                Response.Cookies.Append(
                    AuthConstants.CsrfCookieName,
                    tokens.RequestToken,
                    new CookieOptions
                    {
                        HttpOnly = false,
                        SameSite = SameSiteMode.Strict,
                        Secure = true,
                        Path = "/"
                    });
            }

            return Ok(new { token = tokens.RequestToken });
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();

            return Ok(new {
                userId,
                email,
                roles,
                allClaims = claims
            });
        }

        [HttpGet("saml/login")]
        public async Task<IActionResult> InitiateSamlLogin([FromQuery] string? returnUrl = null)
        {
            try
            {
                var loginUrl = await _samlService.InitiateSamlLoginAsync(returnUrl);
                return Ok(new { loginUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Failed to initiate SAML login", details = ex.Message });
            }
        }

        [HttpPost("saml/acs")]
        public async Task<IActionResult> SamlAssertionConsumerService()
        {
            try
            {
                var principal = await _samlService.ProcessSamlResponseAsync(HttpContext);
                if (principal == null)
                {
                    return BadRequest(new { error = "SAML authentication failed" });
                }

                // Get user information from the principal
                var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var roles = principal.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { error = "Required user information not found in SAML response" });
                }

                // Generate JWT token for the authenticated user
                var token = _jwtGenerator.GenerateToken(userId, email, roles);

                // Log activity for SAML users
                var primaryRole = roles.FirstOrDefault() ?? "Client";
                if (primaryRole == "Client")
                {
                    var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
                    if (client != null)
                    {
                        await _activityService.LogClientLoginAsync(client.ClientId);
                    }
                }

                return Ok(new { token, roles, isSamlUser = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "SAML authentication failed", details = ex.Message });
            }
        }

        [HttpPost("saml/logout")]
        [Authorize]
        public async Task<IActionResult> InitiateSamlLogout()
        {
            try
            {
                var logoutUrl = await _samlService.InitiateSamlLogoutAsync(User);
                return Ok(new { logoutUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Failed to initiate SAML logout", details = ex.Message });
            }
        }
    }

}
