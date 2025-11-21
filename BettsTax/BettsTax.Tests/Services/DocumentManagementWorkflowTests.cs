using BettsTax.Core.Services;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System;
using System.Threading.Tasks;

namespace BettsTax.Tests.Services
{
    public class DocumentManagementWorkflowTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly Mock<ILogger<DocumentManagementWorkflow>> _loggerMock;
        private readonly DocumentManagementWorkflow _documentManagementWorkflow;

        public DocumentManagementWorkflowTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            // Setup mocks
            _notificationServiceMock = new Mock<INotificationService>();
            _auditServiceMock = new Mock<IAuditService>();
            _loggerMock = new Mock<ILogger<DocumentManagementWorkflow>>();

            // Create service instance
            _documentManagementWorkflow = new DocumentManagementWorkflow(
                _context,
                _notificationServiceMock.Object,
                _auditServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task SubmitDocumentAsync_WithValidDocument_ShouldCreateSubmission()
        {
            // Arrange
            int documentId = 1;
            int clientId = 1;
            string documentType = "TaxReturn";
            string userId = "user123";

            // Act
            var result = await _documentManagementWorkflow.SubmitDocumentAsync(documentId, clientId, documentType, userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(documentType, result.Value.DocumentType);
            Assert.Equal(DocumentSubmissionStatus.Submitted, result.Value.Status);
        }

        [Fact]
        public async Task VerifyDocumentAsync_WithValidSubmission_ShouldVerifyDocument()
        {
            // Arrange
            var submission = new DocumentSubmissionWorkflow
            {
                Id = Guid.NewGuid(),
                DocumentId = 1,
                ClientId = 1,
                DocumentType = "TaxReturn",
                Status = DocumentSubmissionStatus.Submitted,
                CreatedAt = DateTime.UtcNow
            };

            _context.DocumentSubmissionWorkflows.Add(submission);
            await _context.SaveChangesAsync();

            string userId = "verifier123";
            string notes = "Document verified successfully";

            // Act
            var result = await _documentManagementWorkflow.VerifyDocumentAsync(submission.Id, userId, notes);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task ApproveDocumentAsync_WithVerifiedDocument_ShouldApproveDocument()
        {
            // Arrange
            var submission = new DocumentSubmissionWorkflow
            {
                Id = Guid.NewGuid(),
                DocumentId = 1,
                ClientId = 1,
                DocumentType = "TaxReturn",
                Status = DocumentSubmissionStatus.VerificationPassed,
                CreatedAt = DateTime.UtcNow
            };

            _context.DocumentSubmissionWorkflows.Add(submission);
            await _context.SaveChangesAsync();

            string userId = "approver123";
            string comments = "Approved";

            // Act
            var result = await _documentManagementWorkflow.ApproveDocumentAsync(submission.Id, userId, comments);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(DocumentSubmissionStatus.Approved, result.Value.Status);
        }

        [Fact]
        public async Task RejectDocumentAsync_WithValidSubmission_ShouldRejectDocument()
        {
            // Arrange
            var submission = new DocumentSubmissionWorkflow
            {
                Id = Guid.NewGuid(),
                DocumentId = 1,
                ClientId = 1,
                DocumentType = "TaxReturn",
                Status = DocumentSubmissionStatus.UnderVerification,
                CreatedAt = DateTime.UtcNow
            };

            _context.DocumentSubmissionWorkflows.Add(submission);
            await _context.SaveChangesAsync();

            string userId = "verifier123";
            string reason = "Document format invalid";

            // Act
            var result = await _documentManagementWorkflow.RejectDocumentAsync(submission.Id, userId, reason);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(DocumentSubmissionStatus.Rejected, result.Value.Status);
        }

        [Fact]
        public async Task CreateDocumentVersionAsync_ShouldCreateNewVersion()
        {
            // Arrange
            var submission = new DocumentSubmissionWorkflow
            {
                Id = Guid.NewGuid(),
                DocumentId = 1,
                ClientId = 1,
                DocumentType = "TaxReturn",
                Status = DocumentSubmissionStatus.Approved,
                CreatedAt = DateTime.UtcNow
            };

            _context.DocumentSubmissionWorkflows.Add(submission);
            await _context.SaveChangesAsync();

            string userId = "user123";
            string fileHash = "abc123def456";

            // Act
            var result = await _documentManagementWorkflow.CreateDocumentVersionAsync(
                submission.Id, userId, fileHash, "Updated tax return");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.VersionNumber);
        }

        [Fact]
        public async Task GetPendingVerificationsAsync_ShouldReturnPendingDocuments()
        {
            // Arrange
            var submission = new DocumentSubmissionWorkflow
            {
                Id = Guid.NewGuid(),
                DocumentId = 1,
                ClientId = 1,
                DocumentType = "TaxReturn",
                Status = DocumentSubmissionStatus.UnderVerification,
                CreatedAt = DateTime.UtcNow
            };

            _context.DocumentSubmissionWorkflows.Add(submission);
            await _context.SaveChangesAsync();

            // Act
            var result = await _documentManagementWorkflow.GetPendingVerificationsAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value);
        }

        [Fact]
        public async Task GetSubmissionStatisticsAsync_ShouldReturnStatistics()
        {
            // Arrange
            var submission = new DocumentSubmissionWorkflow
            {
                Id = Guid.NewGuid(),
                DocumentId = 1,
                ClientId = 1,
                DocumentType = "TaxReturn",
                Status = DocumentSubmissionStatus.Approved,
                CreatedAt = DateTime.UtcNow
            };

            _context.DocumentSubmissionWorkflows.Add(submission);
            await _context.SaveChangesAsync();

            // Act
            var result = await _documentManagementWorkflow.GetSubmissionStatisticsAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(1, result.Value.TotalSubmissions);
            Assert.Equal(1, result.Value.ApprovedCount);
        }

        [Fact]
        public async Task GetDocumentVersionHistoryAsync_ShouldReturnVersionHistory()
        {
            // Arrange
            var submission = new DocumentSubmissionWorkflow
            {
                Id = Guid.NewGuid(),
                DocumentId = 1,
                ClientId = 1,
                DocumentType = "TaxReturn",
                Status = DocumentSubmissionStatus.Approved,
                CreatedAt = DateTime.UtcNow
            };

            _context.DocumentSubmissionWorkflows.Add(submission);
            await _context.SaveChangesAsync();

            // Act
            var result = await _documentManagementWorkflow.GetDocumentVersionHistoryAsync(submission.Id);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }
    }
}

