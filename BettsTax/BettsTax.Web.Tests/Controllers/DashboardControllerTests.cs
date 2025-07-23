using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BettsTax.Web.Tests.Controllers
{
    public class DashboardControllerTests
    {
        private readonly Mock<IDashboardService> _dashboardServiceMock;
        private readonly DashboardController _controller;
        private readonly string _testUserId = "test-user-123";

        public DashboardControllerTests()
        {
            _dashboardServiceMock = new Mock<IDashboardService>();
            _controller = new DashboardController(_dashboardServiceMock.Object);

            // Mock the User.Identity claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId),
                new Claim(ClaimTypes.Email, "test@example.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task GetDashboard_WithValidUser_ShouldReturnOk()
        {
            // Arrange
            var expectedDashboardData = new DashboardDto
            {
                TotalClients = 150,
                ActiveClients = 120,
                ComplianceScore = 85.5m,
                PendingTasks = 12,
                UpcomingDeadlines = 8,
                RecentActivityCount = 25,
                TotalRevenue = 2_500_000,
                ThisMonthRevenue = 350_000
            };

            _dashboardServiceMock.Setup(s => s.GetDashboardDataAsync(_testUserId))
                .ReturnsAsync(expectedDashboardData);

            // Act
            var result = await _controller.GetDashboard();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((bool)response.success).Should().BeTrue();
            
            var data = response.data;
            ((int)data.TotalClients).Should().Be(150);
            ((int)data.ActiveClients).Should().Be(120);
            ((decimal)data.ComplianceScore).Should().Be(85.5m);
            ((int)data.PendingTasks).Should().Be(12);

            _dashboardServiceMock.Verify(s => s.GetDashboardDataAsync(_testUserId), Times.Once);
        }

        [Fact]
        public async Task GetClientSummary_ShouldReturnOk()
        {
            // Arrange
            var expectedSummary = new ClientSummaryDto
            {
                TotalClients = 150,
                NewClientsThisMonth = 8,
                ClientsByCategory = new Dictionary<string, int>
                {
                    { "Large", 25 },
                    { "Medium", 45 },
                    { "Small", 60 },
                    { "Micro", 20 }
                },
                ClientsByStatus = new Dictionary<string, int>
                {
                    { "Active", 120 },
                    { "Inactive", 20 },
                    { "Pending", 10 }
                }
            };

            _dashboardServiceMock.Setup(s => s.GetClientSummaryAsync())
                .ReturnsAsync(expectedSummary);

            // Act
            var result = await _controller.GetClientSummary();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((bool)response.success).Should().BeTrue();

            var data = response.data;
            ((int)data.TotalClients).Should().Be(150);
            ((int)data.NewClientsThisMonth).Should().Be(8);
            data.ClientsByCategory.Should().NotBeNull();
            data.ClientsByStatus.Should().NotBeNull();

            _dashboardServiceMock.Verify(s => s.GetClientSummaryAsync(), Times.Once);
        }

        [Fact]
        public async Task GetComplianceOverview_ShouldReturnOk()
        {
            // Arrange
            var expectedOverview = new ComplianceOverviewDto
            {
                OverallComplianceScore = 88.5m,
                CompliantClients = 125,
                NonCompliantClients = 15,
                ClientsWithWarnings = 10,
                PendingFilings = 22,
                OverdueFilings = 8,
                ComplianceByCategory = new Dictionary<string, decimal>
                {
                    { "Large", 95.2m },
                    { "Medium", 87.8m },
                    { "Small", 82.1m },
                    { "Micro", 75.6m }
                }
            };

            _dashboardServiceMock.Setup(s => s.GetComplianceOverviewAsync())
                .ReturnsAsync(expectedOverview);

            // Act
            var result = await _controller.GetComplianceOverview();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((bool)response.success).Should().BeTrue();

            var data = response.data;
            ((decimal)data.OverallComplianceScore).Should().Be(88.5m);
            ((int)data.CompliantClients).Should().Be(125);
            ((int)data.NonCompliantClients).Should().Be(15);
            ((int)data.PendingFilings).Should().Be(22);

            _dashboardServiceMock.Verify(s => s.GetComplianceOverviewAsync(), Times.Once);
        }

        [Fact]
        public async Task GetRecentActivity_WithDefaultCount_ShouldReturnOk()
        {
            // Arrange
            var expectedActivities = new List<RecentActivityDto>
            {
                new RecentActivityDto
                {
                    Id = 1,
                    ActivityType = "Tax Filing",
                    Description = "Income tax filing submitted for Client ABC Corp",
                    ClientName = "ABC Corp",
                    Timestamp = System.DateTime.UtcNow.AddHours(-2),
                    Status = "Completed"
                },
                new RecentActivityDto
                {
                    Id = 2,
                    ActivityType = "Document Upload",
                    Description = "Financial statements uploaded",
                    ClientName = "XYZ Ltd",
                    Timestamp = System.DateTime.UtcNow.AddHours(-4),
                    Status = "Pending Review"
                }
            };

            _dashboardServiceMock.Setup(s => s.GetRecentActivityAsync(10))
                .ReturnsAsync(expectedActivities);

            // Act
            var result = await _controller.GetRecentActivity();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((bool)response.success).Should().BeTrue();

            var data = response.data as List<RecentActivityDto>;
            data.Should().HaveCount(2);
            data[0].ActivityType.Should().Be("Tax Filing");
            data[1].ActivityType.Should().Be("Document Upload");

            _dashboardServiceMock.Verify(s => s.GetRecentActivityAsync(10), Times.Once);
        }

        [Fact]
        public async Task GetRecentActivity_WithCustomCount_ShouldReturnOk()
        {
            // Arrange
            var expectedActivities = new List<RecentActivityDto>();
            _dashboardServiceMock.Setup(s => s.GetRecentActivityAsync(5))
                .ReturnsAsync(expectedActivities);

            // Act
            var result = await _controller.GetRecentActivity(5);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((bool)response.success).Should().BeTrue();

            _dashboardServiceMock.Verify(s => s.GetRecentActivityAsync(5), Times.Once);
        }

        [Fact]
        public async Task GetUpcomingDeadlines_WithDefaultDays_ShouldReturnOk()
        {
            // Arrange
            var expectedDeadlines = new List<UpcomingDeadlineDto>
            {
                new UpcomingDeadlineDto
                {
                    Id = 1,
                    ClientName = "ABC Corp",
                    TaxType = "Income Tax",
                    DueDate = System.DateTime.UtcNow.AddDays(15),
                    Status = "Pending",
                    Priority = "High",
                    DaysRemaining = 15
                },
                new UpcomingDeadlineDto
                {
                    Id = 2,
                    ClientName = "XYZ Ltd",
                    TaxType = "GST",
                    DueDate = System.DateTime.UtcNow.AddDays(7),
                    Status = "In Progress",
                    Priority = "Medium",
                    DaysRemaining = 7
                }
            };

            _dashboardServiceMock.Setup(s => s.GetUpcomingDeadlinesAsync(30))
                .ReturnsAsync(expectedDeadlines);

            // Act
            var result = await _controller.GetUpcomingDeadlines();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((bool)response.success).Should().BeTrue();

            var data = response.data as List<UpcomingDeadlineDto>;
            data.Should().HaveCount(2);
            data[0].ClientName.Should().Be("ABC Corp");
            data[1].TaxType.Should().Be("GST");

            _dashboardServiceMock.Verify(s => s.GetUpcomingDeadlinesAsync(30), Times.Once);
        }

        [Fact]
        public async Task GetUpcomingDeadlines_WithCustomDays_ShouldReturnOk()
        {
            // Arrange
            var expectedDeadlines = new List<UpcomingDeadlineDto>();
            _dashboardServiceMock.Setup(s => s.GetUpcomingDeadlinesAsync(7))
                .ReturnsAsync(expectedDeadlines);

            // Act
            var result = await _controller.GetUpcomingDeadlines(7);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((bool)response.success).Should().BeTrue();

            _dashboardServiceMock.Verify(s => s.GetUpcomingDeadlinesAsync(7), Times.Once);
        }

        [Fact]
        public async Task GetPendingApprovals_WithValidUser_ShouldReturnOk()
        {
            // Arrange
            var expectedApprovals = new List<PendingApprovalDto>
            {
                new PendingApprovalDto
                {
                    Id = 1,
                    Type = "Tax Filing",
                    ClientName = "ABC Corp",
                    Description = "Income tax filing approval required",
                    SubmittedDate = System.DateTime.UtcNow.AddDays(-2),
                    Priority = "High",
                    Amount = 150_000
                },
                new PendingApprovalDto
                {
                    Id = 2,
                    Type = "Payment",
                    ClientName = "XYZ Ltd",
                    Description = "GST payment approval needed",
                    SubmittedDate = System.DateTime.UtcNow.AddDays(-1),
                    Priority = "Medium",
                    Amount = 75_000
                }
            };

            _dashboardServiceMock.Setup(s => s.GetPendingApprovalsAsync(_testUserId))
                .ReturnsAsync(expectedApprovals);

            // Act
            var result = await _controller.GetPendingApprovals();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((bool)response.success).Should().BeTrue();

            var data = response.data as List<PendingApprovalDto>;
            data.Should().HaveCount(2);
            data[0].Type.Should().Be("Tax Filing");
            data[1].Type.Should().Be("Payment");

            _dashboardServiceMock.Verify(s => s.GetPendingApprovalsAsync(_testUserId), Times.Once);
        }

        [Fact]
        public async Task GetNavigationCounts_WithValidUser_ShouldReturnOk()
        {
            // Arrange
            var expectedCounts = new NavigationCountsDto
            {
                TotalClients = 150,
                PendingDocuments = 25,
                PendingPayments = 12,
                OverdueFilings = 8,
                PendingApprovals = 15,
                UnreadNotifications = 5
            };

            _dashboardServiceMock.Setup(s => s.GetNavigationCountsAsync(_testUserId))
                .ReturnsAsync(expectedCounts);

            // Act
            var result = await _controller.GetNavigationCounts();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((bool)response.success).Should().BeTrue();

            var data = response.data;
            ((int)data.TotalClients).Should().Be(150);
            ((int)data.PendingDocuments).Should().Be(25);
            ((int)data.PendingPayments).Should().Be(12);
            ((int)data.OverdueFilings).Should().Be(8);

            _dashboardServiceMock.Verify(s => s.GetNavigationCountsAsync(_testUserId), Times.Once);
        }

        [Fact]
        public async Task GetDashboard_WithServiceException_ShouldThrow()
        {
            // Arrange
            _dashboardServiceMock.Setup(s => s.GetDashboardDataAsync(_testUserId))
                .ThrowsAsync(new System.Exception("Service error"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(() => _controller.GetDashboard());
        }

        [Fact]
        public async Task GetClientSummary_WithServiceException_ShouldThrow()
        {
            // Arrange
            _dashboardServiceMock.Setup(s => s.GetClientSummaryAsync())
                .ThrowsAsync(new System.Exception("Service error"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(() => _controller.GetClientSummary());
        }

        [Fact]
        public async Task GetComplianceOverview_WithServiceException_ShouldThrow()
        {
            // Arrange
            _dashboardServiceMock.Setup(s => s.GetComplianceOverviewAsync())
                .ThrowsAsync(new System.Exception("Service error"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(() => _controller.GetComplianceOverview());
        }
    }
}