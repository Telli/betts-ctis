using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BettsTax.Core.Services;
using BettsTax.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BettsTax.Core.Tests.Services
{
    public class DashboardServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<DashboardService>> _loggerMock;
        private readonly DashboardService _service;

        public DashboardServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<DashboardService>>();
            _service = new DashboardService(_context, _loggerMock.Object);

            SeedTestData();
        }

        private void SeedTestData()
        {
            // Create test users
            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = "user1",
                    Email = "admin@test.com",
                    FirstName = "Admin",
                    LastName = "User"
                },
                new ApplicationUser
                {
                    Id = "user2",
                    Email = "associate@test.com",
                    FirstName = "Associate",
                    LastName = "User"
                }
            };

            // Create test clients
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
                    IsActive = true,
                    DateCreated = DateTime.UtcNow.AddMonths(-6)
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
                    IsActive = true,
                    DateCreated = DateTime.UtcNow.AddDays(-15)
                },
                new Client
                {
                    Id = 3,
                    BusinessName = "Inactive Corp",
                    CompanyRegistrationNumber = "REG003",
                    Email = "inactive@corp.com",
                    PhoneNumber = "1122334455",
                    Address = "789 Pine St",
                    TaxpayerCategory = TaxpayerCategory.Small,
                    IsActive = false,
                    DateCreated = DateTime.UtcNow.AddMonths(-12)
                }
            };

            // Create test tax years
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
                },
                new TaxYear
                {
                    Id = 2,
                    ClientId = 2,
                    Year = 2024,
                    StartDate = new DateTime(2024, 1, 1),
                    EndDate = new DateTime(2024, 12, 31),
                    Status = "Active"
                }
            };

            // Create test tax filings
            var taxFilings = new List<TaxFiling>
            {
                new TaxFiling
                {
                    Id = 1,
                    ClientId = 1,
                    TaxYearId = 1,
                    TaxType = TaxType.IncomeTax,
                    FilingPeriod = "2024",
                    DueDate = DateTime.UtcNow.AddDays(30),
                    Status = TaxFilingStatus.Pending,
                    TaxableAmount = 2_000_000,
                    TaxAmount = 500_000,
                    DateCreated = DateTime.UtcNow.AddDays(-10)
                },
                new TaxFiling
                {
                    Id = 2,
                    ClientId = 1,
                    TaxYearId = 1,
                    TaxType = TaxType.GST,
                    FilingPeriod = "2024-Q1",
                    DueDate = DateTime.UtcNow.AddDays(-5),
                    Status = TaxFilingStatus.Overdue,
                    TaxableAmount = 1_000_000,
                    TaxAmount = 150_000,
                    DateCreated = DateTime.UtcNow.AddDays(-20)
                },
                new TaxFiling
                {
                    Id = 3,
                    ClientId = 2,
                    TaxYearId = 2,
                    TaxType = TaxType.IncomeTax,
                    FilingPeriod = "2024",
                    DueDate = DateTime.UtcNow.AddDays(15),
                    Status = TaxFilingStatus.InProgress,
                    TaxableAmount = 800_000,
                    TaxAmount = 120_000,
                    DateCreated = DateTime.UtcNow.AddDays(-5)
                }
            };

            // Create test payments
            var payments = new List<Payment>
            {
                new Payment
                {
                    Id = 1,
                    ClientId = 1,
                    TaxYearId = 1,
                    Amount = 500_000,
                    PaymentDate = DateTime.UtcNow.AddDays(-30),
                    PaymentMethod = "Bank Transfer",
                    Status = PaymentStatus.Completed,
                    TransactionReference = "TXN001"
                },
                new Payment
                {
                    Id = 2,
                    ClientId = 2,
                    TaxYearId = 2,
                    Amount = 120_000,
                    PaymentDate = DateTime.UtcNow.AddDays(10),
                    PaymentMethod = "Bank Transfer",
                    Status = PaymentStatus.Pending,
                    TransactionReference = "TXN002"
                }
            };

            _context.Users.AddRange(users);
            _context.Clients.AddRange(clients);
            _context.TaxYears.AddRange(taxYears);
            _context.TaxFilings.AddRange(taxFilings);
            _context.Payments.AddRange(payments);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetDashboardDataAsync_WithValidUserId_ShouldReturnCorrectData()
        {
            // Arrange
            var userId = "user1";

            // Act
            var result = await _service.GetDashboardDataAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.TotalClients.Should().Be(3);
            result.ActiveClients.Should().Be(2);
            result.PendingTasks.Should().BeGreaterThan(0);
            result.UpcomingDeadlines.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetClientSummaryAsync_ShouldReturnCorrectSummary()
        {
            // Act
            var result = await _service.GetClientSummaryAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalClients.Should().Be(3);
            result.NewClientsThisMonth.Should().Be(1); // XYZ Ltd created this month
            result.ClientsByCategory.Should().ContainKey("Large");
            result.ClientsByCategory.Should().ContainKey("Medium");
            result.ClientsByCategory.Should().ContainKey("Small");
            result.ClientsByStatus.Should().ContainKey("Active");
            result.ClientsByStatus.Should().ContainKey("Inactive");
        }

        [Fact]
        public async Task GetComplianceOverviewAsync_ShouldReturnCorrectOverview()
        {
            // Act
            var result = await _service.GetComplianceOverviewAsync();

            // Assert
            result.Should().NotBeNull();
            result.CompliantClients.Should().BeGreaterThanOrEqualTo(0);
            result.NonCompliantClients.Should().BeGreaterThanOrEqualTo(0);
            result.PendingFilings.Should().BeGreaterThan(0);
            result.OverdueFilings.Should().BeGreaterThan(0);
            result.ComplianceByCategory.Should().NotBeNull();
        }

        [Fact]
        public async Task GetRecentActivityAsync_WithCount_ShouldReturnLimitedResults()
        {
            // Arrange
            var count = 2;

            // Act
            var result = await _service.GetRecentActivityAsync(count);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountLessOrEqualTo(count);
        }

        [Fact]
        public async Task GetUpcomingDeadlinesAsync_WithDays_ShouldReturnDeadlinesInRange()
        {
            // Arrange
            var days = 30;

            // Act
            var result = await _service.GetUpcomingDeadlinesAsync(days);

            // Assert
            result.Should().NotBeNull();
            result.All(d => d.DueDate <= DateTime.UtcNow.AddDays(days)).Should().BeTrue();
            result.All(d => d.DueDate >= DateTime.UtcNow.Date).Should().BeTrue();
        }

        [Fact]
        public async Task GetPendingApprovalsAsync_WithUserId_ShouldReturnPendingItems()
        {
            // Arrange
            var userId = "user1";

            // Act
            var result = await _service.GetPendingApprovalsAsync(userId);

            // Assert
            result.Should().NotBeNull();
            // Since we don't have specific approval logic in the test data,
            // we're just checking that the method doesn't throw
        }

        [Fact]
        public async Task GetNavigationCountsAsync_WithUserId_ShouldReturnCorrectCounts()
        {
            // Arrange
            var userId = "user1";

            // Act
            var result = await _service.GetNavigationCountsAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.TotalClients.Should().Be(3);
            result.PendingPayments.Should().BeGreaterThanOrEqualTo(0);
            result.OverdueFilings.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetClientSummaryAsync_WithEmptyDatabase_ShouldReturnZeroCounts()
        {
            // Arrange
            // Clear the database
            _context.Clients.RemoveRange(_context.Clients);
            _context.TaxFilings.RemoveRange(_context.TaxFilings);
            _context.Payments.RemoveRange(_context.Payments);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetClientSummaryAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalClients.Should().Be(0);
            result.NewClientsThisMonth.Should().Be(0);
            result.ClientsByCategory.Should().BeEmpty();
            result.ClientsByStatus.Should().BeEmpty();
        }

        [Fact]
        public async Task GetComplianceOverviewAsync_WithNoFilings_ShouldReturnEmptyCompliance()
        {
            // Arrange
            _context.TaxFilings.RemoveRange(_context.TaxFilings);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetComplianceOverviewAsync();

            // Assert
            result.Should().NotBeNull();
            result.PendingFilings.Should().Be(0);
            result.OverdueFilings.Should().Be(0);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(20)]
        public async Task GetRecentActivityAsync_WithDifferentCounts_ShouldLimitCorrectly(int count)
        {
            // Act
            var result = await _service.GetRecentActivityAsync(count);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountLessOrEqualTo(count);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(30)]
        [InlineData(90)]
        public async Task GetUpcomingDeadlinesAsync_WithDifferentDays_ShouldFilterCorrectly(int days)
        {
            // Act
            var result = await _service.GetUpcomingDeadlinesAsync(days);

            // Assert
            result.Should().NotBeNull();
            var maxDate = DateTime.UtcNow.AddDays(days);
            result.All(d => d.DueDate <= maxDate).Should().BeTrue();
        }

        [Fact]
        public async Task GetDashboardDataAsync_WithNonExistentUser_ShouldStillReturnData()
        {
            // Arrange
            var userId = "non-existent-user";

            // Act
            var result = await _service.GetDashboardDataAsync(userId);

            // Assert
            result.Should().NotBeNull();
            // Should return general dashboard data even if user doesn't exist
            result.TotalClients.Should().BeGreaterThanOrEqualTo(0);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}