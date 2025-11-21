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
    public class PaymentApprovalWorkflowTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly Mock<ILogger<PaymentApprovalWorkflow>> _loggerMock;
        private readonly PaymentApprovalWorkflow _paymentApprovalWorkflow;

        public PaymentApprovalWorkflowTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            // Setup mocks
            _notificationServiceMock = new Mock<INotificationService>();
            _auditServiceMock = new Mock<IAuditService>();
            _loggerMock = new Mock<ILogger<PaymentApprovalWorkflow>>();

            // Create service instance
            _paymentApprovalWorkflow = new PaymentApprovalWorkflow(
                _context,
                _notificationServiceMock.Object,
                _auditServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task RequestPaymentApprovalAsync_WithValidAmount_ShouldCreateApprovalRequest()
        {
            // Arrange
            int paymentId = 1;
            decimal amount = 500000; // 500K SLE
            string userId = "user123";

            // Act
            var result = await _paymentApprovalWorkflow.RequestPaymentApprovalAsync(paymentId, amount, userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(paymentId, result.Value.PaymentId);
            Assert.Equal(amount, result.Value.Amount);
        }

        [Fact]
        public async Task RequestPaymentApprovalAsync_WithSmallAmount_ShouldRequireAssociateApprovalOnly()
        {
            // Arrange
            int paymentId = 1;
            decimal amount = 500000; // Less than 1M
            string userId = "user123";

            // Act
            var result = await _paymentApprovalWorkflow.RequestPaymentApprovalAsync(paymentId, amount, userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            // Should have 1 approval level (Associate)
            Assert.Single(result.Value.ApprovalChain);
        }

        [Fact]
        public async Task RequestPaymentApprovalAsync_WithMediumAmount_ShouldRequireMultipleApprovals()
        {
            // Arrange
            int paymentId = 1;
            decimal amount = 5000000; // 5M SLE (between 1M and 10M)
            string userId = "user123";

            // Act
            var result = await _paymentApprovalWorkflow.RequestPaymentApprovalAsync(paymentId, amount, userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            // Should have 2 approval levels (Associate + Manager)
            Assert.Equal(2, result.Value.ApprovalChain.Count);
        }

        [Fact]
        public async Task RequestPaymentApprovalAsync_WithLargeAmount_ShouldRequireAllApprovals()
        {
            // Arrange
            int paymentId = 1;
            decimal amount = 15000000; // 15M SLE (greater than 10M)
            string userId = "user123";

            // Act
            var result = await _paymentApprovalWorkflow.RequestPaymentApprovalAsync(paymentId, amount, userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            // Should have 3 approval levels (Associate + Manager + Director)
            Assert.Equal(3, result.Value.ApprovalChain.Count);
        }

        [Fact]
        public async Task ApprovePaymentAsync_WithValidApprovalId_ShouldApprovePayment()
        {
            // Arrange
            var approvalRequest = new PaymentApprovalRequest
            {
                Id = Guid.NewGuid(),
                PaymentId = 1,
                Amount = 500000,
                Status = PaymentApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentApprovalRequests.Add(approvalRequest);
            await _context.SaveChangesAsync();

            string userId = "approver123";
            string comments = "Approved";

            // Act
            var result = await _paymentApprovalWorkflow.ApprovePaymentAsync(approvalRequest.Id, userId, comments);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task RejectPaymentAsync_WithValidApprovalId_ShouldRejectPayment()
        {
            // Arrange
            var approvalRequest = new PaymentApprovalRequest
            {
                Id = Guid.NewGuid(),
                PaymentId = 1,
                Amount = 500000,
                Status = PaymentApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentApprovalRequests.Add(approvalRequest);
            await _context.SaveChangesAsync();

            string userId = "approver123";
            string reason = "Insufficient funds";

            // Act
            var result = await _paymentApprovalWorkflow.RejectPaymentAsync(approvalRequest.Id, userId, reason);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task GetPendingApprovalsAsync_ShouldReturnPendingApprovalsForUser()
        {
            // Arrange
            string userId = "approver123";

            var approvalRequest = new PaymentApprovalRequest
            {
                Id = Guid.NewGuid(),
                PaymentId = 1,
                Amount = 500000,
                Status = PaymentApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentApprovalRequests.Add(approvalRequest);
            await _context.SaveChangesAsync();

            // Act
            var result = await _paymentApprovalWorkflow.GetPendingApprovalsAsync(userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task GetApprovalStatisticsAsync_ShouldReturnStatistics()
        {
            // Arrange
            var approvalRequest = new PaymentApprovalRequest
            {
                Id = Guid.NewGuid(),
                PaymentId = 1,
                Amount = 500000,
                Status = PaymentApprovalStatus.Approved,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentApprovalRequests.Add(approvalRequest);
            await _context.SaveChangesAsync();

            // Act
            var result = await _paymentApprovalWorkflow.GetApprovalStatisticsAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(1, result.Value.TotalRequests);
            Assert.Equal(1, result.Value.ApprovedCount);
        }
    }
}

