using System;
using FluentAssertions;
using Xunit;

namespace BettsTax.Data.Tests
{
    public class DocumentTests
    {
        [Fact]
        public void Document_ShouldHaveCorrectDefaultValues()
        {
            // Arrange & Act
            var document = new Document();
            
            // Assert
            document.DocumentId.Should().Be(0);
            document.ClientId.Should().Be(0);
            document.TaxYearId.Should().BeNull();
            document.OriginalFileName.Should().Be(string.Empty);
            document.StoredFileName.Should().Be(string.Empty);
            document.ContentType.Should().Be(string.Empty);
            document.Size.Should().Be(0);
            document.UploadedAt.Date.Should().Be(DateTime.UtcNow.Date);
            document.Client.Should().BeNull();
            document.TaxYear.Should().BeNull();
        }
        
        [Fact]
        public void Document_ShouldSetProperties()
        {
            // Arrange
            var document = new Document();
            var uploadedAt = DateTime.UtcNow.AddDays(-2);
            
            // Act
            document.DocumentId = 42;
            document.ClientId = 101;
            document.TaxYearId = 2022;
            document.OriginalFileName = "test-document.pdf";
            document.StoredFileName = "guid-test-document.pdf";
            document.ContentType = "application/pdf";
            document.Size = 1024;
            document.UploadedAt = uploadedAt;
            
            // Assert
            document.DocumentId.Should().Be(42);
            document.ClientId.Should().Be(101);
            document.TaxYearId.Should().Be(2022);
            document.OriginalFileName.Should().Be("test-document.pdf");
            document.StoredFileName.Should().Be("guid-test-document.pdf");
            document.ContentType.Should().Be("application/pdf");
            document.Size.Should().Be(1024);
            document.UploadedAt.Should().Be(uploadedAt);
        }
        
        [Fact]
        public void Document_ShouldHaveCorrectNavigationProperties()
        {
            // Arrange
            var document = new Document();
            var client = new Client { BusinessName = "Test Business" };
            var taxYear = new TaxYear { Year = 2022 };
            
            // Act
            document.Client = client;
            document.TaxYear = taxYear;
            
            // Assert
            document.Client.Should().NotBeNull();
            document.Client!.BusinessName.Should().Be("Test Business");
            document.TaxYear.Should().NotBeNull();
            document.TaxYear!.Year.Should().Be(2022);
        }
    }
}
