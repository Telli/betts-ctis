using BettsTax.Core.DTOs.Compliance;
using BettsTax.Core.Services;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Data.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BettsTax.Core.Tests.Services;

public class ComplianceEngineIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ComplianceEngine _complianceEngine;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<ComplianceEngine>> _mockLogger;

    public ComplianceEngineIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<ComplianceEngine>>();

        _complianceEngine = new ComplianceEngine(
            _context,
            _mockNotificationService.Object,
            _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add test clients with different compliance scenarios
        var clients = new[]
        {
            new Client 
            { 
                Id = 1, 
                CompanyName = "Compliant Company", 
                TaxpayerCategory = TaxpayerCategory.Large, 
                Status = ClientStatus.Active,
                ComplianceScore = 95.0
            },
            new Client 
            { 
                Id = 2, 
                CompanyName = "Moderate Company", 
                TaxpayerCategory = TaxpayerCategory.Medium, 
                Status = ClientStatus.Active,
                ComplianceScore = 75.0
            },
            new Client 
            { 
                Id = 3, 
                CompanyName = "Non-Compliant Company", 
                TaxpayerCategory = TaxpayerCategory.Small, 
                Status = ClientStatus.Active,
                ComplianceScore = 45.0
            }
        };

        // Add tax years
        var taxYears = new[]
        {
            new TaxYear { Id = 1, Year = 2024, StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 12, 31) }
        };

        // Add tax filings with different compliance statuses
        var taxFilings = new[]
        {
            // Compliant client - filed on time
            new TaxFiling 
            { 
                Id = 1, 
                ClientId = 1, 
                TaxType = TaxType.GST, 
                TaxYearId = 1,
                DueDate = DateTime.UtcNow.AddDays(-30),
                FilingDate = DateTime.UtcNow.AddDays(-35),
                Status = TaxFilingStatus.Filed,
                Amount = 15000
            },
            // Moderate client - filed late
            new TaxFiling 
            { 
                Id = 2, 
                ClientId = 2, 
                TaxType = TaxType.IncomeTax, 
                TaxYearId = 1,
                DueDate = DateTime.UtcNow.AddDays(-20),
                FilingDate = DateTime.UtcNow.AddDays(-10),
                Status = TaxFilingStatus.Filed,
                Amount = 25000
            },
            // Non-compliant client - overdue
            new TaxFiling 
            { 
                Id = 3, 
                ClientId = 3, 
                TaxType = TaxType.GST, 
                TaxYearId = 1,
                DueDate = DateTime.UtcNow.AddDays(-15),
                Status = TaxFilingStatus.Pending,
                Amount = 5000
            }
        };

        // Add payments
        var payments = new[]
        {
            new Payment 
            { 
                Id = 1, 
                ClientId = 1, 
                TaxFilingId = 1,
                Amount = 15000, 
                Status = PaymentStatus.Completed,
                PaymentDate = DateTime.UtcNow.AddDays(-30),
                TaxType = TaxType.GST
            },
            new Payment 
            { 
                Id = 2, 
                ClientId = 2, 
                TaxFilingId = 2,
                Amount = 25000, 
                Status = PaymentStatus.Completed,
                PaymentDate = DateTime.UtcNow.AddDays(-5),
                TaxType = TaxType.IncomeTax
            },
            new Payment 
            { 
                Id = 3, 
                ClientId = 3, 
                TaxFilingId = 3,
                Amount = 5000, 
                Status = PaymentStatus.Pending,
                TaxType = TaxType.GST
            }
        };

        // Add documents
        var documents = new[]
        {
            new Document 
            { 
                Id = 1, 
                ClientId = 1, 
                FileName = "tax-return-2024.pdf",
                DocumentType = DocumentType.TaxReturn,
                Status = DocumentStatus.Approved,
                UploadedAt = DateTime.UtcNow.AddDays(-40)
            },
            new Document 
            { 
                Id = 2, 
                ClientId = 2, 
                FileName = "financial-statements.pdf",
                DocumentType = DocumentType.FinancialStatements,
                Status = DocumentStatus.UnderReview,
                UploadedAt = DateTime.UtcNow.AddDays(-25)
            }
        };

        _context.Clients.AddRange(clients);
        _context.TaxYears.AddRange(taxYears);
        _context.TaxFilings.AddRange(taxFilings);
        _context.Payments.AddRange(payments);
        _context.Documents.AddRange(documents);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CalculateComplianceScoreAsync_ShouldReturnCorrectScore()
    {
        // Arrange
        var clientId = 1; // Compliant client

        // Act
        var result = await _complianceEngine.CalculateComplianceScoreAsync(clientId);

        // Assert
        result.Should().NotBeNull();
        result.ClientId.Should().Be(clientId);
        result.OverallScore.Should().BeGreaterThan(80); // High compliance expected
        result.FilingScore.Should().BeGreaterThan(90); // Filed on time
        result.PaymentScore.Should().BeGreaterThan(90); // Paid on time
        result.DocumentScore.Should().BeGreaterThan(80); // Documents submitted
        result.TimelinessScore.Should().BeGreaterThan(90); // Good timeliness
        result.Level.Should().Be("Green");
    }

    [Fact]
    public async Task CalculateComplianceScoreAsync_NonCompliantClient_ShouldReturnLowScore()
    {
        // Arrange
        var clientId = 3; // Non-compliant client

        // Act
        var result = await _complianceEngine.CalculateComplianceScoreAsync(clientId);

        // Assert
        result.Should().NotBeNull();
        result.ClientId.Should().Be(clientId);
        result.OverallScore.Should().BeLessThan(60); // Low compliance expected
        result.FilingScore.Should().BeLessThan(50); // Overdue filing
        result.PaymentScore.Should().BeLessThan(50); // Unpaid
        result.Level.Should().Be("Red");
    }

    [Fact]
    public async Task GetComplianceAlertsAsync_ShouldReturnActiveAlerts()
    {
        // Arrange
        // Add test compliance alerts
        var alerts = new[]
        {
            new ComplianceAlert
            {
                Id = 1,
                ClientId = 3,
                Type = ComplianceAlertType.DeadlineMissed,
                Severity = AlertSeverity.High,
                Message = "GST filing deadline missed",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new ComplianceAlert
            {
                Id = 2,
                ClientId = 2,
                Type = ComplianceAlertType.DocumentMissing,
                Severity = AlertSeverity.Medium,
                Message = "Financial statements under review",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        _context.ComplianceAlerts.AddRange(alerts);
        await _context.SaveChangesAsync();

        // Act
        var result = await _complianceEngine.GetComplianceAlertsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.IsActive);
        result.Should().BeInDescendingOrder(a => a.CreatedAt);
    }

    [Fact]
    public async Task GetComplianceAlertsAsync_ForSpecificClient_ShouldReturnClientAlerts()
    {
        // Arrange
        var clientId = 3;
        var alerts = new[]
        {
            new ComplianceAlert
            {
                Id = 1,
                ClientId = 3,
                Type = ComplianceAlertType.DeadlineMissed,
                Severity = AlertSeverity.High,
                Message = "GST filing deadline missed",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new ComplianceAlert
            {
                Id = 2,
                ClientId = 2,
                Type = ComplianceAlertType.DocumentMissing,
                Severity = AlertSeverity.Medium,
                Message = "Other client alert",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.ComplianceAlerts.AddRange(alerts);
        await _context.SaveChangesAsync();

        // Act
        var result = await _complianceEngine.GetComplianceAlertsAsync(clientId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().OnlyContain(a => a.ClientId == clientId);
    }

    [Fact]
    public async Task GenerateComplianceAlertsAsync_ShouldCreateAlertsForIssues()
    {
        // Act
        await _complianceEngine.GenerateComplianceAlertsAsync();

        // Assert
        var alerts = await _context.ComplianceAlerts
            .Where(a => a.IsActive)
            .ToListAsync();

        alerts.Should().NotBeEmpty();
        
        // Should have alerts for overdue filings
        alerts.Should().Contain(a => a.Type == ComplianceAlertType.DeadlineMissed);
        
        // Should have alerts for pending payments
        alerts.Should().Contain(a => a.Type == ComplianceAlertType.PaymentOverdue);
    }

    [Fact]
    public async Task GetComplianceTrendsAsync_ShouldReturnTrendData()
    {
        // Arrange
        var clientId = 1;
        var months = 6;

        // Add historical compliance scores
        var historicalScores = new[]
        {
            new ComplianceScore { ClientId = 1, Score = 85.0, Level = "Green", CalculatedAt = DateTime.UtcNow.AddMonths(-3) },
            new ComplianceScore { ClientId = 1, Score = 90.0, Level = "Green", CalculatedAt = DateTime.UtcNow.AddMonths(-2) },
            new ComplianceScore { ClientId = 1, Score = 95.0, Level = "Green", CalculatedAt = DateTime.UtcNow.AddMonths(-1) }
        };

        _context.ComplianceScores.AddRange(historicalScores);
        await _context.SaveChangesAsync();

        // Act
        var result = await _complianceEngine.GetComplianceTrendsAsync(clientId, months);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().BeInAscendingOrder(t => t.Date);
        result.Should().OnlyContain(t => t.ClientId == clientId);
    }

    [Fact]
    public async Task CalculatePenaltiesAsync_ShouldReturnCorrectPenalties()
    {
        // Arrange
        var clientId = 3; // Non-compliant client with overdue filing

        // Act
        var result = await _complianceEngine.CalculatePenaltiesAsync(clientId);

        // Assert
        result.Should().NotBeNull();
        result.ClientId.Should().Be(clientId);
        result.TotalPenalty.Should().BeGreaterThan(0);
        result.PenaltyBreakdown.Should().NotBeEmpty();
        result.PenaltyBreakdown.Should().Contain(p => p.Type == PenaltyType.LateFiling);
    }

    [Fact]
    public async Task UpdateComplianceScoresAsync_ShouldUpdateAllClientScores()
    {
        // Act
        await _complianceEngine.UpdateComplianceScoresAsync();

        // Assert
        var scores = await _context.ComplianceScores
            .Where(s => s.CalculatedAt.Date == DateTime.UtcNow.Date)
            .ToListAsync();

        scores.Should().HaveCount(3); // One for each active client
        scores.Should().OnlyContain(s => s.Score >= 0 && s.Score <= 100);
        scores.Should().OnlyContain(s => !string.IsNullOrEmpty(s.Level));
    }

    [Fact]
    public async Task ResolveComplianceAlertAsync_ShouldDeactivateAlert()
    {
        // Arrange
        var alert = new ComplianceAlert
        {
            Id = 1,
            ClientId = 2,
            Type = ComplianceAlertType.DocumentMissing,
            Severity = AlertSeverity.Medium,
            Message = "Document missing",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ComplianceAlerts.Add(alert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _complianceEngine.ResolveComplianceAlertAsync(1, "Issue resolved", "test-user");

        // Assert
        result.Should().BeTrue();
        
        var resolvedAlert = await _context.ComplianceAlerts.FindAsync(1);
        resolvedAlert.Should().NotBeNull();
        resolvedAlert.IsActive.Should().BeFalse();
        resolvedAlert.ResolvedAt.Should().NotBeNull();
        resolvedAlert.ResolvedBy.Should().Be("test-user");
        resolvedAlert.Resolution.Should().Be("Issue resolved");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}