using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using BettsTax.Web.Models;

namespace BettsTax.Web.Services
{
    /// <summary>
    /// Authentication service implementation using JWT
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthenticationService> _logger;

        // In-memory user store for demo purposes
        // In production, replace with database access (EF Core, Dapper, etc.)
        private static readonly List<User> _demoUsers = new()
        {
            new User
            {
                UserId = "staff-001",
                Email = "staff@bettsfirm.com",
                // BCrypt hash of "password" - in production, use proper password hashing
                PasswordHash = "$2a$11$8gF3z5VJZQx5YxYvK8Zv8ePqF3ZxJZDKJ6F5YxYvK8Zv8ePqF3ZxJ",
                Role = "Staff",
                ClientId = null,
                ClientName = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            },
            new User
            {
                UserId = "client-001",
                Email = "client@example.com",
                PasswordHash = "$2a$11$8gF3z5VJZQx5YxYvK8Zv8ePqF3ZxJZDKJ6F5YxYvK8Zv8ePqF3ZxJ",
                Role = "Client",
                ClientId = 1,
                ClientName = "ABC Corporation",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            },
            new User
            {
                UserId = "client-002",
                Email = "john@xyztrad.com",
                PasswordHash = "$2a$11$8gF3z5VJZQx5YxYvK8Zv8ePqF3ZxJZDKJ6F5YxYvK8Zv8ePqF3ZxJ",
                Role = "Client",
                ClientId = 2,
                ClientName = "XYZ Trading",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            },
            new User
            {
                UserId = "admin-001",
                Email = "admin@bettsfirm.com",
                PasswordHash = "$2a$11$8gF3z5VJZQx5YxYvK8Zv8ePqF3ZxJZDKJ6F5YxYvK8Zv8ePqF3ZxJ",
                Role = "Admin",
                ClientId = null,
                ClientName = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-1)
            }
        };

        public AuthenticationService(
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthenticationService> logger)
        {
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<LoginResponse> AuthenticateAsync(LoginRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Email and password are required"
                    };
                }

                // Find user by email
                var user = _demoUsers.FirstOrDefault(u =>
                    u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase));

                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                // Verify password - for demo, accepting "password" for all users
                // In production, use BCrypt.Verify(request.Password, user.PasswordHash)
                if (request.Password != "password")
                {
                    _logger.LogWarning("Failed password verification for user: {UserId}", user.UserId);
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;

                // Generate JWT token
                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();
                var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

                _logger.LogInformation("Successful login for user: {UserId} with role: {Role}",
                    user.UserId, user.Role);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    User = new UserInfo
                    {
                        UserId = user.UserId,
                        Email = user.Email,
                        Role = user.Role,
                        ClientId = user.ClientId,
                        ClientName = user.ClientName
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication for email: {Email}", request.Email);
                return new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred during authentication"
                };
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return await Task.FromResult(false);
            }
        }

        public async Task<UserInfo?> GetUserFromTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var role = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                var clientId = jwtToken.Claims.FirstOrDefault(c => c.Type == "ClientId")?.Value;
                var clientName = jwtToken.Claims.FirstOrDefault(c => c.Type == "ClientName")?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
                {
                    return null;
                }

                return await Task.FromResult(new UserInfo
                {
                    UserId = userId,
                    Email = email ?? string.Empty,
                    Role = role ?? string.Empty,
                    ClientId = int.TryParse(clientId, out var cId) ? cId : null,
                    ClientName = clientName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting user from token");
                return null;
            }
        }

        public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
        {
            // Simplified refresh token implementation
            // In production, store refresh tokens in database with expiration
            _logger.LogWarning("Refresh token functionality not fully implemented");

            return await Task.FromResult(new LoginResponse
            {
                Success = false,
                Message = "Refresh token functionality not implemented"
            });
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add client-specific claims if user is a client
            if (user.ClientId.HasValue)
            {
                claims.Add(new Claim("ClientId", user.ClientId.Value.ToString()));
                if (!string.IsNullOrEmpty(user.ClientName))
                {
                    claims.Add(new Claim("ClientName", user.ClientName));
                }
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
