using BettsTax.Core.DTOs.KPI;
using BettsTax.Core.Services;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Data.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;

namespace BettsTax.Core.Tests.Services;

public class KPIServiceIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly KPIService _kpiService;
    private readonly Mock<IClientService> _mockClientService;
    private readonly Mock<ITaxFilingService> _mockTaxFilingService;
    private readonly Mock<IPaymentService> _mockPaymentService;
    private readonly Mock<IDocumentService> _mockDocumentService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<KPIService>> _mockLogger;

    public KPIServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        
        _mockClientService = new Mock<IClientService>();
        _mockTaxFilingService = new Mock<ITaxFilingService>();
        _mockPaymentService = new Mock<IPaymentService>();
        _mockDocumentService = new Mock<IDocumentService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<KPIService>>();

        _kpiService = new KPIService(
            _mockClientService.Object,
            _mockTaxFilingService.Object,
            _mockPaymentService.Object,
            _mockDocumentService.Object,
            _mockNotificationService.Object,
            _mockCache.Object,
            _mockLogger.Object,
            _context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add test clients
        var clients = new[]
        {
            new Client { Id = 1, CompanyName = "Test Company 1", TaxpayerCategory = TaxpayerCategory.Large, Status = ClientStatus.Active },
            new Client { Id = 2, CompanyName = "Test Company 2", TaxpayerCategory = TaxpayerCategory.Medium, Status = ClientStatus.Active },
            new Client { Id = 3, CompanyName = "Test Company 3", TaxpayerCategory = TaxpayerCategory.Small, Status = ClientStatus.Inactive }
        };

        // Add test KPI metrics
        var kpiMetrics = new[]
        {
            new KPIMetric { Id = 1, MetricName = "ComplianceRate", Value = 85.5, RecordedAt = DateTime.UtcNow, ClientId = 1 },
            new KPIMetric { Id = 2, MetricName = "FilingTimeliness", Value = 90.0, RecordedAt = DateTime.UtcNow, ClientId = 1 },
            new KPIMetric { Id = 3, MetricName = "PaymentCompletion", Value = 95.0, RecordedAt = DateTime.UtcNow, ClientId = 2 }
        };

        // Add test compliance scores
        var complianceScores = new[]
        {
            new ComplianceScore { Id = 1, ClientId = 1, Score = 85.5, Level = "Green", CalculatedAt = DateTime.UtcNow },
            new ComplianceScore { Id = 2, ClientId = 2, Score = 70.0, Level = "Yellow", CalculatedAt = DateTime.UtcNow }
        };

        _context.Clients.AddRange(clients);
        _context.KPIMetrics.AddRange(kpiMetrics);
        _context.ComplianceScores.AddRange(complianceScores);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetInternalKPIsAsync_ShouldReturnValidMetrics()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;

        // Act
        var result = await _kpiService.GetInternalKPIsAsync(fromDate, toDate);

        // Assert
        result.Should().NotBeNull();
        result.TotalActiveClients.Should().Be(2);
        result.ComplianceRate.Should().BeGreaterThan(0);
        result.FilingTimeliness.Should().BeGreaterThan(0);
        result.PaymentCompletionRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetClientKPIsAsync_ShouldReturnClientSpecificMetrics()
    {
        // Arrange
        var clientId = 1;

        // Act
        var result = await _kpiService.GetClientKPIsAsync(clientId);

        // Assert
        result.Should().NotBeNull();
        result.ClientId.Should().Be(clientId);
        result.ComplianceScore.Should().Be(85.5);
        result.ComplianceLevel.Should().Be("Green");
    }

    [Fact]
    public async Task GetKPIAlertsAsync_ShouldReturnActiveAlerts()
    {
        // Arrange
        // Add test alerts to database
        var alerts = new[]
        {
            new ComplianceAlert 
            { 
                Id = 1, 
                ClientId = 2, 
                Type = ComplianceAlertType.DeadlineMissed, 
                Message = "Tax filing deadline missed",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
        _context.ComplianceAlerts.AddRange(alerts);
        await _context.SaveChangesAsync();

        // Act
        var result = await _kpiService.GetKPIAlertsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Message.Should().Be("Tax filing deadline missed");
    }

    [Fact]
    public async Task UpdateKPIThresholdsAsync_ShouldUpdateSuccessfully()
    {
        // Arrange
        var thresholds = new Dictionary<string, double>
        {
            { "ComplianceRate", 80.0 },
            { "FilingTimeliness", 85.0 }
        };

        // Act
        var result = await _kpiService.UpdateKPIThresholdsAsync(thresholds);

        // Assert
        result.Should().BeTrue();
        
        // Verify thresholds were saved to database
        var savedThresholds = await _context.KPIThresholds.ToListAsync();
        savedThresholds.Should().HaveCount(2);
        savedThresholds.Should().Contain(t => t.MetricName == "ComplianceRate" && t.ThresholdValue == 80.0);
    }

    [Fact]
    public async Task GenerateKPIAlertsAsync_ShouldCreateAlertsForThresholdBreaches()
    {
        // Arrange
        // Set up threshold that will be breached
        _context.KPIThresholds.Add(new KPIThreshold 
        { 
            MetricName = "ComplianceRate", 
            ThresholdValue = 90.0,
            AlertLevel = "High"
        });
        await _context.SaveChangesAsync();

        // Act
        await _kpiService.GenerateKPIAlertsAsync();

        // Assert
        var alerts = await _context.ComplianceAlerts.Where(a => a.IsActive).ToListAsync();
        alerts.Should().NotBeEmpty();
        alerts.Should().Contain(a => a.Type == ComplianceAlertType.KPIThresholdBreach);
    }

    [Fact]
    public async Task GetKPITrendsAsync_ShouldReturnTrendData()
    {
        // Arrange
        var clientId = 1;
        var days = 30;

        // Add historical KPI data
        var historicalData = new[]
        {
            new KPIMetric { MetricName = "ComplianceRate", Value = 80.0, RecordedAt = DateTime.UtcNow.AddDays(-20), ClientId = clientId },
            new KPIMetric { MetricName = "ComplianceRate", Value = 85.0, RecordedAt = DateTime.UtcNow.AddDays(-15), ClientId = clientId },
            new KPIMetric { MetricName = "ComplianceRate", Value = 87.0, RecordedAt = DateTime.UtcNow.AddDays(-10), ClientId = clientId }
        };
        _context.KPIMetrics.AddRange(historicalData);
        await _context.SaveChangesAsync();

        // Act
        var result = await _kpiService.GetKPITrendsAsync(clientId, days);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().BeInAscendingOrder(t => t.Date);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}