using System.Security.Claims;
using BettsTax.Data;
using BettsTax.Web.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sustainsys.Saml2.AspNetCore2;

namespace BettsTax.Web.Services;

public interface ISamlAuthenticationService
{
    Task<string> InitiateSamlLoginAsync(string? returnUrl = null);
    Task<ClaimsPrincipal> ProcessSamlResponseAsync(HttpContext context);
    Task<string> InitiateSamlLogoutAsync(ClaimsPrincipal user);
    Task<bool> ValidateSamlConfigurationAsync();
}

public class SamlAuthenticationService : ISamlAuthenticationService
{
    private readonly IOptions<SamlOptions> _samlOptions;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SamlAuthenticationService> _logger;
    private readonly JwtTokenGenerator _jwtTokenGenerator;

    public SamlAuthenticationService(
        IOptions<SamlOptions> samlOptions,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        ILogger<SamlAuthenticationService> logger,
        JwtTokenGenerator jwtTokenGenerator)
    {
        _samlOptions = samlOptions;
        _userManager = userManager;
        _dbContext = dbContext;
        _logger = logger;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<string> InitiateSamlLoginAsync(string? returnUrl = null)
    {
        try
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = returnUrl ?? "/",
                IsPersistent = true
            };

            var challengeUrl = $"/Saml2/SignIn?ReturnUrl={Uri.EscapeDataString(returnUrl ?? "/")}";
            return challengeUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating SAML login");
            throw;
        }
    }

    public async Task<ClaimsPrincipal> ProcessSamlResponseAsync(HttpContext context)
    {
        try
        {
            var result = await context.AuthenticateAsync(Saml2Defaults.Scheme);
            if (!result.Succeeded)
            {
                _logger.LogWarning("SAML authentication failed");
                return null;
            }

            var principal = result.Principal;
            if (principal == null)
            {
                _logger.LogWarning("SAML principal is null");
                return null;
            }

            // Extract user information from SAML claims
            var nameId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var givenName = principal.FindFirst(ClaimTypes.GivenName)?.Value;
            var surname = principal.FindFirst(ClaimTypes.Surname)?.Value;

            if (string.IsNullOrEmpty(nameId) || string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Required SAML claims (NameID or Email) are missing");
                return null;
            }

            // Find or create user
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = givenName,
                    LastName = surname,
                    EmailConfirmed = true, // SAML-authenticated users are considered verified
                    IsSamlUser = true,
                    SamlNameId = nameId
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("Failed to create SAML user: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return null;
                }

                // Assign default role for SAML users
                await _userManager.AddToRoleAsync(user, "Client");
            }
            else
            {
                // Update SAML information for existing user
                user.SamlNameId = nameId;
                user.IsSamlUser = true;
                await _userManager.UpdateAsync(user);
            }

            // Create new claims principal with our user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            if (!string.IsNullOrEmpty(user.FirstName))
                claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
            if (!string.IsNullOrEmpty(user.LastName))
                claims.Add(new Claim(ClaimTypes.Surname, user.LastName));

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "SAML");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _logger.LogInformation("SAML authentication successful for user {Email}", email);
            return claimsPrincipal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SAML response");
            throw;
        }
    }

    public async Task<string> InitiateSamlLogoutAsync(ClaimsPrincipal user)
    {
        try
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/"
            };

            var logoutUrl = $"/Saml2/SignOut?ReturnUrl={Uri.EscapeDataString("/")}";

            // Sign out from local session
            // The SAML logout will be handled by the IdP

            return logoutUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating SAML logout");
            throw;
        }
    }

    public async Task<bool> ValidateSamlConfigurationAsync()
    {
        try
        {
            var options = _samlOptions.Value;

            if (string.IsNullOrEmpty(options.EntityId) ||
                string.IsNullOrEmpty(options.MetadataUrl) ||
                string.IsNullOrEmpty(options.SignOnUrl) ||
                string.IsNullOrEmpty(options.LogoutUrl))
            {
                _logger.LogWarning("SAML configuration is incomplete");
                return false;
            }

            // Additional validation could include checking certificate file existence
            // and attempting to load SAML metadata

            _logger.LogInformation("SAML configuration validation successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SAML configuration");
            return false;
        }
    }
}