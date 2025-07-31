using BettsTax.Core.DTOs.Reports;
using BettsTax.Core.Services;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Data.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;

namespace BettsTax.Core.Tests.Services;

public class ReportServiceIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ReportService _reportService;
    private readonly Mock<IReportTemplateService> _mockTemplateService;
    private readonly Mock<IReportGenerator> _mockReportGenerator;
    private readonly Mock<IScheduler> _mockScheduler;
    private readonly Mock<ILogger<ReportService>> _mockLogger;

    public ReportServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        
        _mockTemplateService = new Mock<IReportTemplateService>();
        _mockReportGenerator = new Mock<IReportGenerator>();
        _mockScheduler = new Mock<IScheduler>();
        _mockLogger = new Mock<ILogger<ReportService>>();

        _reportService = new ReportService(
            _context,
            _mockTemplateService.Object,
            _mockReportGenerator.Object,
            _mockScheduler.Object,
            _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add test user
        var user = new ApplicationUser 
        { 
            Id = "test-user-1", 
            UserName = "testuser@example.com",
            Email = "testuser@example.com"
        };

        // Add test clients
        var clients = new[]
        {
            new Client { Id = 1, CompanyName = "Test Company 1", Status = ClientStatus.Active },
            new Client { Id = 2, CompanyName = "Test Company 2", Status = ClientStatus.Active }
        };

        // Add test tax filings
        var taxFilings = new[]
        {
            new TaxFiling 
            { 
                Id = 1, 
                ClientId = 1, 
                TaxType = TaxType.GST, 
                Amount = 10000, 
                Status = TaxFilingStatus.Filed,
                FilingDate = DateTime.UtcNow.AddDays(-10)
            },
            new TaxFiling 
            { 
                Id = 2, 
                ClientId = 1, 
                TaxType = TaxType.IncomeTax, 
                Amount = 25000, 
                Status = TaxFilingStatus.Pending,
                DueDate = DateTime.UtcNow.AddDays(15)
            }
        };

        // Add test payments
        var payments = new[]
        {
            new Payment 
            { 
                Id = 1, 
                ClientId = 1, 
                Amount = 10000, 
                Status = PaymentStatus.Completed,
                PaymentDate = DateTime.UtcNow.AddDays(-5),
                TaxType = TaxType.GST
            }
        };

        _context.Users.Add(user);
        _context.Clients.AddRange(clients);
        _context.TaxFilings.AddRange(taxFilings);
        _context.Payments.AddRange(payments);
        _context.SaveChanges();
    }

    [Fact]
    public async Task QueueReportGenerationAsync_ShouldCreateReportRequest()
    {
        // Arrange
        var request = new CreateReportRequestDto
        {
            Type = ReportType.TaxFilingReport,
            Format = ReportFormat.PDF,
            Parameters = new Dictionary<string, object>
            {
                { "ClientId", 1 },
                { "FromDate", DateTime.UtcNow.AddMonths(-1) },
                { "ToDate", DateTime.UtcNow }
            }
        };
        var userId = "test-user-1";

        // Act
        var requestId = await _reportService.QueueReportGenerationAsync(request, userId);

        // Assert
        requestId.Should().NotBeNullOrEmpty();
        
        var reportRequest = await _context.ReportRequests
            .FirstOrDefaultAsync(r => r.RequestId == requestId);
        
        reportRequest.Should().NotBeNull();
        reportRequest.Type.Should().Be(ReportType.TaxFilingReport);
        reportRequest.Format.Should().Be(ReportFormat.PDF);
        reportRequest.Status.Should().Be(ReportStatus.Pending);
        reportRequest.RequestedByUserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetReportStatusAsync_ShouldReturnCorrectStatus()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();
        var reportRequest = new ReportRequest
        {
            RequestId = requestId,
            Type = ReportType.PaymentReport,
            Format = ReportFormat.Excel,
            Status = ReportStatus.InProgress,
            Progress = 50,
            RequestedByUserId = "test-user-1",
            RequestedAt = DateTime.UtcNow
        };
        
        _context.ReportRequests.Add(reportRequest);
        await _context.SaveChangesAsync();

        // Act
        var status = await _reportService.GetReportStatusAsync(requestId);

        // Assert
        status.Should().NotBeNull();
        status.RequestId.Should().Be(requestId);
        status.Status.Should().Be(ReportStatus.InProgress);
        status.Progress.Should().Be(50);
    }

    [Fact]
    public async Task GetUserReportHistoryAsync_ShouldReturnUserReports()
    {
        // Arrange
        var userId = "test-user-1";
        var reports = new[]
        {
            new ReportRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Type = ReportType.ComplianceReport,
                Format = ReportFormat.PDF,
                Status = ReportStatus.Completed,
                RequestedByUserId = userId,
                RequestedAt = DateTime.UtcNow.AddDays(-5),
                CompletedAt = DateTime.UtcNow.AddDays(-4),
                FilePath = "reports/compliance-report.pdf"
            },
            new ReportRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Type = ReportType.TaxFilingReport,
                Format = ReportFormat.Excel,
                Status = ReportStatus.Failed,
                RequestedByUserId = userId,
                RequestedAt = DateTime.UtcNow.AddDays(-3),
                ErrorMessage = "Data validation failed"
            }
        };

        _context.ReportRequests.AddRange(reports);
        await _context.SaveChangesAsync();

        // Act
        var history = await _reportService.GetUserReportHistoryAsync(userId, 1, 10);

        // Assert
        history.Should().NotBeNull();
        history.Items.Should().HaveCount(2);
        history.Items.Should().BeInDescendingOrder(r => r.RequestedAt);
        history.Items.Should().OnlyContain(r => r.RequestedByUserId == userId);
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();
        var reportRequest = new ReportRequest
        {
            RequestId = requestId,
            Type = ReportType.TaxFilingReport,
            Format = ReportFormat.PDF,
            Status = ReportStatus.Pending,
            Parameters = "{\"ClientId\": 1, \"FromDate\": \"2024-01-01\", \"ToDate\": \"2024-12-31\"}",
            RequestedByUserId = "test-user-1",
            RequestedAt = DateTime.UtcNow
        };
        
        _context.ReportRequests.Add(reportRequest);
        await _context.SaveChangesAsync();

        // Mock successful report generation
        _mockReportGenerator
            .Setup(x => x.GeneratePdfReportAsync(It.IsAny<ReportData>(), It.IsAny<string>()))
            .ReturnsAsync("reports/generated-report.pdf");

        // Act
        await _reportService.GenerateReportAsync(requestId);

        // Assert
        var updatedRequest = await _context.ReportRequests
            .FirstOrDefaultAsync(r => r.RequestId == requestId);
        
        updatedRequest.Should().NotBeNull();
        updatedRequest.Status.Should().Be(ReportStatus.Completed);
        updatedRequest.FilePath.Should().NotBeNullOrEmpty();
        updatedRequest.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReportDataAsync_ShouldReturnCorrectData()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "ClientId", 1 },
            { "FromDate", DateTime.UtcNow.AddMonths(-1) },
            { "ToDate", DateTime.UtcNow }
        };

        // Act
        var reportData = await _reportService.GetReportDataAsync(ReportType.TaxFilingReport, parameters);

        // Assert
        reportData.Should().NotBeNull();
        reportData.TaxFilings.Should().NotBeEmpty();
        reportData.TaxFilings.Should().OnlyContain(tf => tf.ClientId == 1);
        reportData.Payments.Should().NotBeEmpty();
        reportData.Client.Should().NotBeNull();
        reportData.Client.Id.Should().Be(1);
    }

    [Fact]
    public async Task ScheduleRecurringReportAsync_ShouldCreateSchedule()
    {
        // Arrange
        var scheduleRequest = new ScheduleReportDto
        {
            ReportType = ReportType.ComplianceReport,
            Format = ReportFormat.PDF,
            Schedule = "0 0 1 * *", // Monthly on the 1st
            Parameters = new Dictionary<string, object> { { "ClientId", 1 } },
            EmailRecipients = new[] { "admin@example.com" }
        };
        var userId = "test-user-1";

        // Act
        var scheduleId = await _reportService.ScheduleRecurringReportAsync(scheduleRequest, userId);

        // Assert
        scheduleId.Should().NotBeNullOrEmpty();
        
        var schedule = await _context.ReportSchedules
            .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);
        
        schedule.Should().NotBeNull();
        schedule.ReportType.Should().Be(ReportType.ComplianceReport);
        schedule.CronExpression.Should().Be("0 0 1 * *");
        schedule.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteReportAsync_ShouldRemoveReportAndFile()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();
        var reportRequest = new ReportRequest
        {
            RequestId = requestId,
            Type = ReportType.PaymentReport,
            Format = ReportFormat.PDF,
            Status = ReportStatus.Completed,
            FilePath = "reports/test-report.pdf",
            RequestedByUserId = "test-user-1",
            RequestedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
        
        _context.ReportRequests.Add(reportRequest);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reportService.DeleteReportAsync(requestId, "test-user-1");

        // Assert
        result.Should().BeTrue();
        
        var deletedReport = await _context.ReportRequests
            .FirstOrDefaultAsync(r => r.RequestId == requestId);
        
        deletedReport.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}