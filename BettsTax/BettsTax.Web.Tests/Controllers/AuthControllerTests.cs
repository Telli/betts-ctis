using System.Collections.Generic;
using System.Threading.Tasks;
using BettsTax.Data;
using BettsTax.Web.Controllers;
using BettsTax.Web.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BettsTax.Web.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<JwtTokenGenerator> _jwtGeneratorMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            // Mock UserManager (requires store mock for constructor)
            var storeMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                storeMock.Object, null, null, null, null, null, null, null, null);
                
            _jwtGeneratorMock = new Mock<JwtTokenGenerator>(null);
            _controller = new AuthController(_userManagerMock.Object, _jwtGeneratorMock.Object);
        }

        [Fact]
        public async Task Register_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var registerDto = new RegisterDto(
                FirstName: "John",
                LastName: "Doe",
                Email: "john.doe@example.com",
                Password: "P@ssw0rd123!"
            );

            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            result.Should().BeOfType<OkResult>();
            _userManagerMock.Verify(um => um.CreateAsync(
                It.Is<ApplicationUser>(u => 
                    u.Email == registerDto.Email && 
                    u.UserName == registerDto.Email && 
                    u.FirstName == registerDto.FirstName && 
                    u.LastName == registerDto.LastName), 
                registerDto.Password), 
                Times.Once);
        }

        [Fact]
        public async Task Register_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto(
                FirstName: "John",
                LastName: "Doe",
                Email: "john.doe@example.com",
                Password: "weak"
            );

            var errors = new List<IdentityError> { new IdentityError { Code = "PasswordTooShort", Description = "Password is too short" } };
            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(errors.ToArray()));

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var returnedErrors = badRequestResult.Value.Should().BeAssignableTo<IEnumerable<IdentityError>>().Subject;
            returnedErrors.Should().HaveCount(1);
            returnedErrors.Should().Contain(e => e.Code == "PasswordTooShort");
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnOkWithToken()
        {
            // Arrange
            var loginDto = new LoginDto(
                Email: "john.doe@example.com",
                Password: "P@ssw0rd123!"
            );

            var user = new ApplicationUser
            {
                Id = "user123",
                Email = loginDto.Email,
                UserName = loginDto.Email,
                FirstName = "John",
                LastName = "Doe"
            };

            var roles = new List<string> { "User" };
            var token = "jwt-token-string";

            _userManagerMock.Setup(um => um.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.CheckPasswordAsync(user, loginDto.Password))
                .ReturnsAsync(true);
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(roles);
            _jwtGeneratorMock.Setup(jg => jg.GenerateToken(user.Id, user.Email, roles))
                .Returns(token);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic tokenResponse = okResult.Value;
            ((string)tokenResponse.token).Should().Be(token);
            
            _userManagerMock.Verify(um => um.FindByEmailAsync(loginDto.Email), Times.Once);
            _userManagerMock.Verify(um => um.CheckPasswordAsync(user, loginDto.Password), Times.Once);
            _userManagerMock.Verify(um => um.GetRolesAsync(user), Times.Once);
            _jwtGeneratorMock.Verify(jg => jg.GenerateToken(user.Id, user.Email, roles), Times.Once);
        }

        [Fact]
        public async Task Login_WithNonExistingUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto(
                Email: "nonexistent@example.com",
                Password: "P@ssw0rd123!"
            );

            _userManagerMock.Setup(um => um.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
            _userManagerMock.Verify(um => um.FindByEmailAsync(loginDto.Email), Times.Once);
            _userManagerMock.Verify(um => um.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Login_WithIncorrectPassword_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto(
                Email: "john.doe@example.com",
                Password: "WrongPassword123!"
            );

            var user = new ApplicationUser
            {
                Id = "user123",
                Email = loginDto.Email,
                UserName = loginDto.Email
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.CheckPasswordAsync(user, loginDto.Password))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
            _userManagerMock.Verify(um => um.FindByEmailAsync(loginDto.Email), Times.Once);
            _userManagerMock.Verify(um => um.CheckPasswordAsync(user, loginDto.Password), Times.Once);
            _userManagerMock.Verify(um => um.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }
    }
}
