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

        public AuthController(
            UserManager<ApplicationUser> userManager, 
            JwtTokenGenerator jwtGenerator,
            IActivityTimelineService activityService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _jwtGenerator = jwtGenerator;
            _activityService = activityService;
            _context = context;
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
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized();
            if (!await _userManager.CheckPasswordAsync(user, dto.Password)) return Unauthorized();
            
            // Update last login date
            user.LastLoginDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            
            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtGenerator.GenerateToken(user.Id, user.Email!, roles);
            
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
            
            return Ok(new { token, roles }); // Include roles in response for debugging
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
    }

    public record RegisterDto(string FirstName, string LastName, string Email, string Password);
    public record LoginDto(string Email, string Password);
}
