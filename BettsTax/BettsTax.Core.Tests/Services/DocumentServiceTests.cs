using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BettsTax.Core.Tests.Services
{
    public class DocumentServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IFileStorageService> _fileStorageServiceMock;
        private readonly Mock<ILogger<DocumentService>> _loggerMock;
        private readonly DocumentService _service;

        public DocumentServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _fileStorageServiceMock = new Mock<IFileStorageService>();
            _loggerMock = new Mock<ILogger<DocumentService>>();
            _service = new DocumentService(_context, _fileStorageServiceMock.Object, _loggerMock.Object);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var clients = new List<Client>
            {
                new Client
                {
                    Id = 1,
                    BusinessName = "ABC Corp",
                    CompanyRegistrationNumber = "REG001",
                    Email = "abc@corp.com",
                    PhoneNumber = "1234567890",
                    Address = "123 Main St",
                    TaxpayerCategory = TaxpayerCategory.Large,
                    IsActive = true
                },
                new Client
                {
                    Id = 2,
                    BusinessName = "XYZ Ltd",
                    CompanyRegistrationNumber = "REG002",
                    Email = "xyz@ltd.com",
                    PhoneNumber = "0987654321",
                    Address = "456 Oak Ave",
                    TaxpayerCategory = TaxpayerCategory.Medium,
                    IsActive = true
                }
            };

            var taxYears = new List<TaxYear>
            {
                new TaxYear
                {
                    Id = 1,
                    ClientId = 1,
                    Year = 2024,
                    StartDate = new DateTime(2024, 1, 1),
                    EndDate = new DateTime(2024, 12, 31),
                    Status = "Active"
                }
            };

            var documents = new List<Document>
            {
                new Document
                {
                    Id = 1,
                    ClientId = 1,
                    TaxYearId = 1,
                    FileName = "financial_statements_2024.pdf",
                    DocumentType = "Financial Statements",
                    FilePath = "/uploads/client1/financial_statements_2024.pdf",
                    FileSize = 1024000,
                    MimeType = "application/pdf",
                    UploadDate = DateTime.UtcNow.AddDays(-10),
                    Status = "Approved"
                },
                new Document
                {
                    Id = 2,
                    ClientId = 1,
                    TaxYearId = 1,
                    FileName = "tax_return_draft.pdf",
                    DocumentType = "Tax Return",
                    FilePath = "/uploads/client1/tax_return_draft.pdf",
                    FileSize = 512000,
                    MimeType = "application/pdf",
                    UploadDate = DateTime.UtcNow.AddDays(-5),
                    Status = "Pending Review"
                }
            };

            _context.Clients.AddRange(clients);
            _context.TaxYears.AddRange(taxYears);
            _context.Documents.AddRange(documents);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetDocumentsAsync_WithValidClientId_ShouldReturnDocuments()
        {
            // Arrange
            var clientId = 1;

            // Act
            var result = await _service.GetDocumentsAsync(clientId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(d => d.ClientId == clientId).Should().BeTrue();
        }

        [Fact]
        public async Task GetDocumentsAsync_WithInvalidClientId_ShouldReturnEmpty()
        {
            // Arrange
            var clientId = 999;

            // Act
            var result = await _service.GetDocumentsAsync(clientId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetDocumentByIdAsync_WithValidId_ShouldReturnDocument()
        {
            // Arrange
            var documentId = 1;

            // Act
            var result = await _service.GetDocumentByIdAsync(documentId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(documentId);
            result.FileName.Should().Be("financial_statements_2024.pdf");
            result.DocumentType.Should().Be("Financial Statements");
        }

        [Fact]
        public async Task GetDocumentByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var documentId = 999;

            // Act
            var result = await _service.GetDocumentByIdAsync(documentId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UploadDocumentAsync_WithValidFile_ShouldUploadSuccessfully()
        {
            // Arrange
            var clientId = 1;
            var taxYearId = 1;
            var documentType = "Bank Statement";
            
            var fileMock = new Mock<IFormFile>();
            var content = "Hello World from a Fake File";
            var fileName = "bank_statement.pdf";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);
            fileMock.Setup(_ => _.ContentType).Returns("application/pdf");

            var expectedFilePath = $"/uploads/client{clientId}/{fileName}";
            _fileStorageServiceMock.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync(expectedFilePath);

            // Act
            var result = await _service.UploadDocumentAsync(clientId, taxYearId, documentType, fileMock.Object);

            // Assert
            result.Should().NotBeNull();
            result.ClientId.Should().Be(clientId);
            result.TaxYearId.Should().Be(taxYearId);
            result.DocumentType.Should().Be(documentType);
            result.FileName.Should().Be(fileName);
            result.FilePath.Should().Be(expectedFilePath);

            // Verify the document was saved to database
            var savedDocument = await _context.Documents.FirstOrDefaultAsync(d => d.FileName == fileName);
            savedDocument.Should().NotBeNull();
            savedDocument.ClientId.Should().Be(clientId);
        }

        [Fact]
        public async Task UploadDocumentAsync_WithFileStorageFailure_ShouldThrowException()
        {
            // Arrange
            var clientId = 1;
            var taxYearId = 1;
            var documentType = "Bank Statement";
            
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(_ => _.FileName).Returns("test.pdf");
            fileMock.Setup(_ => _.Length).Returns(1000);

            _fileStorageServiceMock.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("File storage error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => 
                _service.UploadDocumentAsync(clientId, taxYearId, documentType, fileMock.Object));
        }

        [Fact]
        public async Task UpdateDocumentStatusAsync_WithValidId_ShouldUpdateStatus()
        {
            // Arrange
            var documentId = 2;
            var newStatus = "Approved";
            var userId = "test-user";

            // Act
            var result = await _service.UpdateDocumentStatusAsync(documentId, newStatus, userId);

            // Assert
            result.Should().BeTrue();

            var updatedDocument = await _context.Documents.FindAsync(documentId);
            updatedDocument.Should().NotBeNull();
            updatedDocument.Status.Should().Be(newStatus);
        }

        [Fact]
        public async Task UpdateDocumentStatusAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var documentId = 999;
            var newStatus = "Approved";
            var userId = "test-user";

            // Act
            var result = await _service.UpdateDocumentStatusAsync(documentId, newStatus, userId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteDocumentAsync_WithValidId_ShouldDeleteDocument()
        {
            // Arrange
            var documentId = 1;
            var document = await _context.Documents.FindAsync(documentId);
            var filePath = document.FilePath;

            _fileStorageServiceMock.Setup(s => s.DeleteFileAsync(filePath))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteDocumentAsync(documentId);

            // Assert
            result.Should().BeTrue();

            var deletedDocument = await _context.Documents.FindAsync(documentId);
            deletedDocument.Should().BeNull();

            _fileStorageServiceMock.Verify(s => s.DeleteFileAsync(filePath), Times.Once);
        }

        [Fact]
        public async Task DeleteDocumentAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var documentId = 999;

            // Act
            var result = await _service.DeleteDocumentAsync(documentId);

            // Assert
            result.Should().BeFalse();
            _fileStorageServiceMock.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetDocumentsByTaxYearAsync_WithValidTaxYearId_ShouldReturnDocuments()
        {
            // Arrange
            var taxYearId = 1;

            // Act
            var result = await _service.GetDocumentsByTaxYearAsync(taxYearId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(d => d.TaxYearId == taxYearId).Should().BeTrue();
        }

        [Fact]
        public async Task GetDocumentsByTypeAsync_WithValidType_ShouldReturnFilteredDocuments()
        {
            // Arrange
            var clientId = 1;
            var documentType = "Financial Statements";

            // Act
            var result = await _service.GetDocumentsByTypeAsync(clientId, documentType);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().DocumentType.Should().Be(documentType);
        }

        [Fact]
        public async Task GetDocumentsByStatusAsync_WithValidStatus_ShouldReturnFilteredDocuments()
        {
            // Arrange
            var status = "Pending Review";

            // Act
            var result = await _service.GetDocumentsByStatusAsync(status);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Status.Should().Be(status);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task UploadDocumentAsync_WithInvalidDocumentType_ShouldThrowException(string documentType)
        {
            // Arrange
            var clientId = 1;
            var taxYearId = 1;
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(_ => _.FileName).Returns("test.pdf");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.UploadDocumentAsync(clientId, taxYearId, documentType, fileMock.Object));
        }

        [Fact]
        public async Task UploadDocumentAsync_WithNullFile_ShouldThrowException()
        {
            // Arrange
            var clientId = 1;
            var taxYearId = 1;
            var documentType = "Test Document";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.UploadDocumentAsync(clientId, taxYearId, documentType, null));
        }

        [Fact]
        public async Task GetPendingDocumentsAsync_ShouldReturnOnlyPendingDocuments()
        {
            // Act
            var result = await _service.GetPendingDocumentsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.All(d => d.Status == "Pending Review").Should().BeTrue();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}