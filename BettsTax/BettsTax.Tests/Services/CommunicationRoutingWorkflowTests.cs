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
    public class CommunicationRoutingWorkflowTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly Mock<ILogger<CommunicationRoutingWorkflow>> _loggerMock;
        private readonly CommunicationRoutingWorkflow _communicationRoutingWorkflow;

        public CommunicationRoutingWorkflowTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            // Setup mocks
            _notificationServiceMock = new Mock<INotificationService>();
            _auditServiceMock = new Mock<IAuditService>();
            _loggerMock = new Mock<ILogger<CommunicationRoutingWorkflow>>();

            // Create service instance
            _communicationRoutingWorkflow = new CommunicationRoutingWorkflow(
                _context,
                _notificationServiceMock.Object,
                _auditServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task ReceiveAndRouteMessageAsync_WithValidMessage_ShouldCreateRouting()
        {
            // Arrange
            int clientId = 1;
            string messageType = "Inquiry";
            string subject = "Tax Question";
            string content = "I have a question about tax filing";
            string priority = "Normal";
            string channel = "Email";
            string sentBy = "user123";

            // Act
            var result = await _communicationRoutingWorkflow.ReceiveAndRouteMessageAsync(
                clientId, messageType, subject, content, priority, channel, sentBy);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(messageType, result.Value.MessageType);
            Assert.Equal(subject, result.Value.Subject);
        }

        [Fact]
        public async Task ReceiveAndRouteMessageAsync_WithCriticalPriority_ShouldMarkAsHighPriority()
        {
            // Arrange
            int clientId = 1;
            string messageType = "Complaint";
            string subject = "Urgent Issue";
            string content = "Critical issue needs immediate attention";
            string priority = "Critical";
            string channel = "Phone";
            string sentBy = "user123";

            // Act
            var result = await _communicationRoutingWorkflow.ReceiveAndRouteMessageAsync(
                clientId, messageType, subject, content, priority, channel, sentBy);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("Critical", result.Value.Priority);
        }

        [Fact]
        public async Task AssignMessageAsync_WithValidRoutingId_ShouldAssignMessage()
        {
            // Arrange
            var routing = new CommunicationRoutingWorkflow
            {
                Id = Guid.NewGuid(),
                ClientId = 1,
                MessageType = "Inquiry",
                Subject = "Tax Question",
                Content = "I have a question",
                Priority = CommunicationPriority.Normal,
                Status = CommunicationRoutingStatus.Received,
                Channel = "Email",
                SentBy = "user123",
                ReceivedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.CommunicationRoutingWorkflows.Add(routing);
            await _context.SaveChangesAsync();

            string assignToUserId = "handler123";
            string notes = "Assigned to handler";

            // Act
            var result = await _communicationRoutingWorkflow.AssignMessageAsync(routing.Id, assignToUserId, notes);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(assignToUserId, result.Value.AssignedTo);
        }

        [Fact]
        public async Task EscalateMessageAsync_WithValidRoutingId_ShouldEscalateMessage()
        {
            // Arrange
            var routing = new CommunicationRoutingWorkflow
            {
                Id = Guid.NewGuid(),
                ClientId = 1,
                MessageType = "Complaint",
                Subject = "Issue",
                Content = "Problem description",
                Priority = CommunicationPriority.High,
                Status = CommunicationRoutingStatus.Assigned,
                Channel = "Email",
                SentBy = "user123",
                AssignedTo = "handler123",
                AssignedAt = DateTime.UtcNow.AddHours(-2),
                ReceivedAt = DateTime.UtcNow.AddHours(-3),
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            };

            _context.CommunicationRoutingWorkflows.Add(routing);
            await _context.SaveChangesAsync();

            string escalatedBy = "handler123";
            string reason = "Unable to resolve";

            // Act
            var result = await _communicationRoutingWorkflow.EscalateMessageAsync(routing.Id, escalatedBy, reason);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(CommunicationRoutingStatus.Escalated, result.Value.Status);
        }

        [Fact]
        public async Task ResolveMessageAsync_WithValidRoutingId_ShouldResolveMessage()
        {
            // Arrange
            var routing = new CommunicationRoutingWorkflow
            {
                Id = Guid.NewGuid(),
                ClientId = 1,
                MessageType = "Inquiry",
                Subject = "Question",
                Content = "Question content",
                Priority = CommunicationPriority.Normal,
                Status = CommunicationRoutingStatus.Assigned,
                Channel = "Email",
                SentBy = "user123",
                AssignedTo = "handler123",
                AssignedAt = DateTime.UtcNow.AddHours(-1),
                ReceivedAt = DateTime.UtcNow.AddHours(-2),
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            };

            _context.CommunicationRoutingWorkflows.Add(routing);
            await _context.SaveChangesAsync();

            string resolvedBy = "handler123";
            string resolutionNotes = "Issue resolved";

            // Act
            var result = await _communicationRoutingWorkflow.ResolveMessageAsync(routing.Id, resolvedBy, resolutionNotes);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(CommunicationRoutingStatus.Resolved, result.Value.Status);
            Assert.NotNull(result.Value.ResolvedAt);
        }

        [Fact]
        public async Task GetPendingMessagesAsync_ShouldReturnPendingMessages()
        {
            // Arrange
            string userId = "handler123";

            var routing = new CommunicationRoutingWorkflow
            {
                Id = Guid.NewGuid(),
                ClientId = 1,
                MessageType = "Inquiry",
                Subject = "Question",
                Content = "Question content",
                Priority = CommunicationPriority.Normal,
                Status = CommunicationRoutingStatus.Assigned,
                Channel = "Email",
                SentBy = "user123",
                AssignedTo = userId,
                AssignedAt = DateTime.UtcNow,
                ReceivedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.CommunicationRoutingWorkflows.Add(routing);
            await _context.SaveChangesAsync();

            // Act
            var result = await _communicationRoutingWorkflow.GetPendingMessagesAsync(userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value);
        }

        [Fact]
        public async Task GetCommunicationStatisticsAsync_ShouldReturnStatistics()
        {
            // Arrange
            var routing = new CommunicationRoutingWorkflow
            {
                Id = Guid.NewGuid(),
                ClientId = 1,
                MessageType = "Inquiry",
                Subject = "Question",
                Content = "Question content",
                Priority = CommunicationPriority.Normal,
                Status = CommunicationRoutingStatus.Resolved,
                Channel = "Email",
                SentBy = "user123",
                ReceivedAt = DateTime.UtcNow.AddHours(-2),
                ResolvedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            };

            _context.CommunicationRoutingWorkflows.Add(routing);
            await _context.SaveChangesAsync();

            // Act
            var result = await _communicationRoutingWorkflow.GetCommunicationStatisticsAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(1, result.Value.TotalMessages);
            Assert.Equal(1, result.Value.ResolvedCount);
        }

        [Fact]
        public async Task CheckAndApplyEscalationRulesAsync_ShouldEscalateExpiredMessages()
        {
            // Arrange
            var routing = new CommunicationRoutingWorkflow
            {
                Id = Guid.NewGuid(),
                ClientId = 1,
                MessageType = "Complaint",
                Subject = "Issue",
                Content = "Problem",
                Priority = CommunicationPriority.High,
                Status = CommunicationRoutingStatus.Assigned,
                Channel = "Email",
                SentBy = "user123",
                AssignedTo = "handler123",
                AssignedAt = DateTime.UtcNow.AddHours(-3), // 3 hours ago
                ReceivedAt = DateTime.UtcNow.AddHours(-4),
                CreatedAt = DateTime.UtcNow.AddHours(-4)
            };

            _context.CommunicationRoutingWorkflows.Add(routing);
            await _context.SaveChangesAsync();

            // Act
            var result = await _communicationRoutingWorkflow.CheckAndApplyEscalationRulesAsync();

            // Assert
            Assert.True(result.IsSuccess);
        }
    }
}

