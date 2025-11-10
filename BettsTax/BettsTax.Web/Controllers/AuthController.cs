using BettsTax.Core.Services;
using BettsTax.Data;
using BettsTax.Web.Services;
using Microsoft.AspNetCore.Authorization;
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
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager, 
            JwtTokenGenerator jwtGenerator,
            IActivityTimelineService activityService,
            ApplicationDbContext context,
            ISamlAuthenticationService samlService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _jwtGenerator = jwtGenerator;
            _activityService = activityService;
            _context = context;
            _samlService = samlService;
            _logger = logger;
        }

        [HttpPost("register")]
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
                
                _logger.LogInformation("Login successful for: {Email}", dto.Email);
                return Ok(new { token, roles }); // Include roles in response for debugging
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for email: {Email}", dto.Email);
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
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

    public record RegisterDto(string FirstName, string LastName, string Email, string Password);
    public record LoginDto(string Email, string Password);
}
