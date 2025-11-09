using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using BettsTax.Web.Services;

namespace BettsTax.Tests.Security
{
    /// <summary>
    /// Security tests for Authorization Service
    /// </summary>
    public class AuthorizationServiceTests
    {
        private readonly Mock<ILogger<AuthorizationService>> _loggerMock;
        private readonly AuthorizationService _authService;

        public AuthorizationServiceTests()
        {
            _loggerMock = new Mock<ILogger<AuthorizationService>>();
            _authService = new AuthorizationService(_loggerMock.Object);
        }

        private ClaimsPrincipal CreateUser(string userId, string role, int? clientId = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, $"{userId}@example.com"),
                new Claim(ClaimTypes.Role, role)
            };

            if (clientId.HasValue)
            {
                claims.Add(new Claim("ClientId", clientId.Value.ToString()));
            }

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        [Fact]
        public void CanAccessClientData_AdminUser_CanAccessAnyClient()
        {
            // Arrange
            var adminUser = CreateUser("admin-001", "Admin");
            var clientId = 123;

            // Act
            var canAccess = _authService.CanAccessClientData(adminUser, clientId);

            // Assert
            Assert.True(canAccess);
        }

        [Fact]
        public void CanAccessClientData_StaffUser_CanAccessAnyClient()
        {
            // Arrange
            var staffUser = CreateUser("staff-001", "Staff");
            var clientId = 123;

            // Act
            var canAccess = _authService.CanAccessClientData(staffUser, clientId);

            // Assert
            Assert.True(canAccess);
        }

        [Fact]
        public void CanAccessClientData_ClientUser_CanAccessOwnData()
        {
            // Arrange
            var clientUser = CreateUser("client-001", "Client", clientId: 123);
            var requestedClientId = 123;

            // Act
            var canAccess = _authService.CanAccessClientData(clientUser, requestedClientId);

            // Assert
            Assert.True(canAccess);
        }

        [Fact]
        public void CanAccessClientData_ClientUser_CannotAccessOtherClientData()
        {
            // Arrange
            var clientUser = CreateUser("client-001", "Client", clientId: 123);
            var otherClientId = 456;

            // Act
            var canAccess = _authService.CanAccessClientData(clientUser, otherClientId);

            // Assert
            Assert.False(canAccess);
            // Verify warning was logged
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Access denied")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void CanAccessClientData_NoClientIdSpecified_AllowsAccess()
        {
            // Arrange
            var clientUser = CreateUser("client-001", "Client", clientId: 123);

            // Act
            var canAccess = _authService.CanAccessClientData(clientUser, null);

            // Assert
            Assert.True(canAccess);
        }

        [Fact]
        public void GetUserClientId_ClientUser_ReturnsClientId()
        {
            // Arrange
            var clientUser = CreateUser("client-001", "Client", clientId: 123);

            // Act
            var clientId = _authService.GetUserClientId(clientUser);

            // Assert
            Assert.Equal(123, clientId);
        }

        [Fact]
        public void GetUserClientId_StaffUser_ReturnsNull()
        {
            // Arrange
            var staffUser = CreateUser("staff-001", "Staff");

            // Act
            var clientId = _authService.GetUserClientId(staffUser);

            // Assert
            Assert.Null(clientId);
        }

        [Fact]
        public void IsStaffOrAdmin_AdminUser_ReturnsTrue()
        {
            // Arrange
            var adminUser = CreateUser("admin-001", "Admin");

            // Act
            var isStaffOrAdmin = _authService.IsStaffOrAdmin(adminUser);

            // Assert
            Assert.True(isStaffOrAdmin);
        }

        [Fact]
        public void IsStaffOrAdmin_StaffUser_ReturnsTrue()
        {
            // Arrange
            var staffUser = CreateUser("staff-001", "Staff");

            // Act
            var isStaffOrAdmin = _authService.IsStaffOrAdmin(staffUser);

            // Assert
            Assert.True(isStaffOrAdmin);
        }

        [Fact]
        public void IsStaffOrAdmin_ClientUser_ReturnsFalse()
        {
            // Arrange
            var clientUser = CreateUser("client-001", "Client", clientId: 123);

            // Act
            var isStaffOrAdmin = _authService.IsStaffOrAdmin(clientUser);

            // Assert
            Assert.False(isStaffOrAdmin);
        }

        [Fact]
        public void GetUserRole_ValidUser_ReturnsRole()
        {
            // Arrange
            var user = CreateUser("user-001", "TestRole");

            // Act
            var role = _authService.GetUserRole(user);

            // Assert
            Assert.Equal("TestRole", role);
        }

        [Fact]
        public void CanAccessClientData_NullUser_ReturnsFalse()
        {
            // Act
            var canAccess = _authService.CanAccessClientData(null!, 123);

            // Assert
            Assert.False(canAccess);
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("admin")]
        [InlineData("ADMIN")]
        [InlineData("Staff")]
        [InlineData("staff")]
        [InlineData("STAFF")]
        public void IsStaffOrAdmin_CaseInsensitive_ReturnsTrue(string role)
        {
            // Arrange
            var user = CreateUser("user-001", role);

            // Act
            var isStaffOrAdmin = _authService.IsStaffOrAdmin(user);

            // Assert
            Assert.True(isStaffOrAdmin);
        }
    }
}
