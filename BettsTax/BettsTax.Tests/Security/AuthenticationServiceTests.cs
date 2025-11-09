using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using BettsTax.Web.Models;
using BettsTax.Web.Services;

namespace BettsTax.Tests.Security
{
    /// <summary>
    /// Security tests for Authentication Service
    /// </summary>
    public class AuthenticationServiceTests
    {
        private readonly Mock<ILogger<AuthenticationService>> _loggerMock;
        private readonly JwtSettings _jwtSettings;
        private readonly AuthenticationService _authService;

        public AuthenticationServiceTests()
        {
            _loggerMock = new Mock<ILogger<AuthenticationService>>();
            _jwtSettings = new JwtSettings
            {
                Secret = "ThisIsATestSecretKeyForJWTTokenGenerationWithAtLeast32Characters",
                Issuer = "BettsTaxTest",
                Audience = "BettsTaxTestAudience",
                ExpirationMinutes = 60
            };

            var options = Options.Create(_jwtSettings);
            _authService = new AuthenticationService(options, _loggerMock.Object);
        }

        [Fact]
        public async Task AuthenticateAsync_ValidCredentials_ReturnsSuccess()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "staff@bettsfirm.com",
                Password = "password"
            };

            // Act
            var result = await _authService.AuthenticateAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Token);
            Assert.NotNull(result.User);
            Assert.Equal("Staff", result.User.Role);
        }

        [Fact]
        public async Task AuthenticateAsync_InvalidEmail_ReturnsFailure()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "nonexistent@example.com",
                Password = "password"
            };

            // Act
            var result = await _authService.AuthenticateAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Token);
            Assert.Equal("Invalid email or password", result.Message);
        }

        [Fact]
        public async Task AuthenticateAsync_InvalidPassword_ReturnsFailure()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "staff@bettsfirm.com",
                Password = "wrongpassword"
            };

            // Act
            var result = await _authService.AuthenticateAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task AuthenticateAsync_EmptyEmail_ReturnsFailure()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "",
                Password = "password"
            };

            // Act
            var result = await _authService.AuthenticateAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("required", result.Message?.ToLower());
        }

        [Fact]
        public async Task AuthenticateAsync_EmptyPassword_ReturnsFailure()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "staff@bettsfirm.com",
                Password = ""
            };

            // Act
            var result = await _authService.AuthenticateAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("required", result.Message?.ToLower());
        }

        [Fact]
        public async Task AuthenticateAsync_ClientCredentials_ReturnsClientRole()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "client@example.com",
                Password = "password"
            };

            // Act
            var result = await _authService.AuthenticateAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Client", result.User?.Role);
            Assert.NotNull(result.User?.ClientId);
        }

        [Fact]
        public async Task ValidateTokenAsync_ValidToken_ReturnsTrue()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Email = "staff@bettsfirm.com",
                Password = "password"
            };
            var loginResult = await _authService.AuthenticateAsync(loginRequest);

            // Act
            var isValid = await _authService.ValidateTokenAsync(loginResult.Token!);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public async Task ValidateTokenAsync_InvalidToken_ReturnsFalse()
        {
            // Arrange
            var invalidToken = "invalid.token.string";

            // Act
            var isValid = await _authService.ValidateTokenAsync(invalidToken);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public async Task GetUserFromTokenAsync_ValidToken_ReturnsUserInfo()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Email = "client@example.com",
                Password = "password"
            };
            var loginResult = await _authService.AuthenticateAsync(loginRequest);

            // Act
            var userInfo = await _authService.GetUserFromTokenAsync(loginResult.Token!);

            // Assert
            Assert.NotNull(userInfo);
            Assert.Equal("client@example.com", userInfo.Email);
            Assert.Equal("Client", userInfo.Role);
            Assert.NotNull(userInfo.ClientId);
        }
    }
}
