using System;
using FluentAssertions;
using Xunit;

namespace BettsTax.Data.Tests
{
    public class ApplicationUserTests
    {
        [Fact]
        public void ApplicationUser_ShouldHaveCorrectDefaultValues()
        {
            // Arrange & Act
            var user = new ApplicationUser();
            
            // Assert
            user.FirstName.Should().Be(string.Empty);
            user.LastName.Should().Be(string.Empty);
            user.IsActive.Should().BeTrue();
            user.CreatedDate.Date.Should().Be(DateTime.UtcNow.Date);
            user.LastLoginDate.Should().BeNull();
        }
        
        [Fact]
        public void ApplicationUser_ShouldSetProperties()
        {
            // Arrange
            var user = new ApplicationUser();
            var loginDate = DateTime.UtcNow.AddDays(-1);
            
            // Act
            user.FirstName = "John";
            user.LastName = "Doe";
            user.IsActive = false;
            user.LastLoginDate = loginDate;
            
            // Assert
            user.FirstName.Should().Be("John");
            user.LastName.Should().Be("Doe");
            user.IsActive.Should().BeFalse();
            user.LastLoginDate.Should().Be(loginDate);
        }
        
        [Fact]
        public void ApplicationUser_ShouldInheritIdentityUserProperties()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "test-id",
                UserName = "jdoe",
                Email = "john.doe@example.com",
                PhoneNumber = "123-456-7890"
            };
            
            // Act & Assert
            user.Id.Should().Be("test-id");
            user.UserName.Should().Be("jdoe");
            user.Email.Should().Be("john.doe@example.com");
            user.PhoneNumber.Should().Be("123-456-7890");
        }
    }
}
