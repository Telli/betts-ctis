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
    public class ComplianceMonitoringWorkflowTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly Mock<ILogger<ComplianceMonitoringWorkflow>> _loggerMock;
        private readonly ComplianceMonitoringWorkflow _complianceMonitoringWorkflow;

        public ComplianceMonitoringWorkflowTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            // Setup mocks
            _notificationServiceMock = new Mock<INotificationService>();
            _auditServiceMock = new Mock<IAuditService>();
            _loggerMock = new Mock<ILogger<ComplianceMonitoringWorkflow>>();

            // Create service instance
            _complianceMonitoringWorkflow = new ComplianceMonitoringWorkflow(
                _context,
                _notificationServiceMock.Object,
                _auditServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task MonitorDeadlinesAsync_ShouldCheckAllPendingItems()
        {
            // Arrange
            var compliance = new ComplianceMonitoringWorkflow
            {
                Id = Guid.NewGuid(),
                ClientId = 1,
                ComplianceType = "TaxFiling",
                DueDate = DateTime.UtcNow.AddDays(7),
                Status = ComplianceMonitoringStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.ComplianceMonitoringWorkflows.Add(compliance);
            await _context.SaveChangesAsync();

            // Act
            var result = await _complianceMonitoringWorkflow.MonitorDeadlinesAsync();

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task CalculatePenaltyAsync_WithLateFilingPenalty_ShouldCalculateCorrectly()
        {
            // Arrange
            var compliance = new ComplianceMonitoringWorkflow
            {
                Id = Guid.NewGuid(),
                ClientId = 1,
                ComplianceType = "TaxFiling",
                DueDate = DateTime.UtcNow.AddMonths(-2), // 2 months overdue
                Amount = 1000000,
                Status = ComplianceMonitoringStatus.Overdue,
                CreatedAt = DateTime.UtcNow
            };

            _context.ComplianceMonitoringWorkflows.Add(compliance);
            await _context.SaveChangesAsync();

            // Act
            var result = await _complianceMonitoringWorkflow.CalculatePenaltyAsync(compliance.Id, "LateFilingPenalty");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            // Late filing penalty: 5% per month = 10% for 2 months = 100,000
            Assert.Equal(100000, result.Value.PenaltyAmount);
        }

        [Fact]
        public async Task CalculatePenaltyAsync_WithLatePaymentPenalty_ShouldCalculateCorrectly()
        {
            // Arrange
            var compliance = new ComplianceMonitoringWorkflow
            {
                Id = Guid.NewGuid(),
                ClientId = 1,
                ComplianceType = "TaxPayment",
                DueDate = DateTime.UtcNow.AddMonths(-1), // 1 month overdue
                Amount = 1000000,
                Status = ComplianceMonitoringStatus.Overdue,
                CreatedAt = DateTime.UtcNow
            };

            _context.ComplianceMonitoringWorkflows.Add(compliance);
            await _context.SaveChangesAsync();

            // Act
            var result = await _complianceMonitoringWorkflow.CalculatePenaltyAsync(compliance.Id, "LatePaymentPenalty");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            // Late payment penalty: 2% per month = 2% for 1 month = 20,000
            Assert.Equal(20000, result.Value.PenaltyAmount);
        }

        [Fact]
        public async Task GenerateComplianceAlertAsync_ShouldCreateAlert()
        {
            // Arrange
            var compliance = new ComplianceMonitoringWorkflow
            {
                Id = Guid.NewGuid(),
                ClientId = 1,
                ComplianceType = "TaxFiling",
                DueDate = DateTime.UtcNow.AddDays(7),
                Status = ComplianceMonitoringStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.ComplianceMonitoringWorkflows.Add(compliance);
            await _context.SaveChangesAsync();

            // Act
            var result = await _complianceMonitoringWorkflow.GenerateComplianceAlertAsync(
                compliance.Id, "7DayWarning", "Tax filing due in 7 days");

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task MarkAsFiledAsync_ShouldUpdateStatus()
        {
            // Arrange
            var compliance = new ComplianceMonitoringWorkflow
            {
                Id = Guid.NewGuid(),
                ClientId = 1,
                ComplianceType = "TaxFiling",
                DueDate = DateTime.UtcNow.AddDays(7),
                Status = ComplianceMonitoringStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.ComplianceMonitoringWorkflows.Add(compliance);
            await _context.SaveChangesAsync();

            // Act
            var result = await _complianceMonitoringWorkflow.MarkAsFiledAsync(compliance.Id, DateTime.UtcNow);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(ComplianceMonitoringStatus.Filed, result.Value.Status);
        }

        [Fact]
        public async Task MarkAsPaidAsync_ShouldUpdateStatus()
        {
            // Arrange
            var compliance = new ComplianceMonitoringWorkflow
            {
                Id = Guid.NewGuid(),
                ClientId = 1,
                ComplianceType = "TaxPayment",
                DueDate = DateTime.UtcNow.AddDays(7),
                Amount = 1000000,
                Status = ComplianceMonitoringStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.ComplianceMonitoringWorkflows.Add(compliance);
            await _context.SaveChangesAsync();

            // Act
            var result = await _complianceMonitoringWorkflow.MarkAsPaidAsync(compliance.Id, DateTime.UtcNow, 1000000);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(ComplianceMonitoringStatus.Paid, result.Value.Status);
        }

        [Fact]
        public async Task GetPendingComplianceAsync_ShouldReturnPendingItems()
        {
            // Arrange
            var compliance = new ComplianceMonitoringWorkflow
            {
                Id = Guid.NewGuid(),
                ClientId = 1,
                ComplianceType = "TaxFiling",
                DueDate = DateTime.UtcNow.AddDays(7),
                Status = ComplianceMonitoringStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.ComplianceMonitoringWorkflows.Add(compliance);
            await _context.SaveChangesAsync();

            // Act
            var result = await _complianceMonitoringWorkflow.GetPendingComplianceAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value);
        }

        [Fact]
        public async Task GetComplianceStatisticsAsync_ShouldReturnStatistics()
        {
            // Arrange
            var compliance = new ComplianceMonitoringWorkflow
            {
                Id = Guid.NewGuid(),
                ClientId = 1,
                ComplianceType = "TaxFiling",
                DueDate = DateTime.UtcNow.AddDays(7),
                Status = ComplianceMonitoringStatus.Compliant,
                CreatedAt = DateTime.UtcNow
            };

            _context.ComplianceMonitoringWorkflows.Add(compliance);
            await _context.SaveChangesAsync();

            // Act
            var result = await _complianceMonitoringWorkflow.GetComplianceStatisticsAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(1, result.Value.TotalItems);
            Assert.Equal(1, result.Value.CompliantCount);
        }
    }
}

