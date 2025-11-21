using BettsTax.Core.Services;
using BettsTax.Data;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BettsTax.Core.Tests.Services
{
    public class SierraLeoneTaxCalculationServiceTests
    {
        private readonly SierraLeoneTaxCalculationService _service;
        private readonly Mock<ILogger<SierraLeoneTaxCalculationService>> _mockLogger;
        private readonly Mock<ISystemSettingService> _mockSettingService;

        public SierraLeoneTaxCalculationServiceTests()
        {
            _mockLogger = new Mock<ILogger<SierraLeoneTaxCalculationService>>();
            _mockSettingService = new Mock<ISystemSettingService>();
            _mockSettingService
                .Setup(s => s.GetSettingAsync<decimal?>(It.IsAny<string>()))
                .ReturnsAsync((decimal?)null);
            _mockSettingService
                .Setup(s => s.GetSettingAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            _service = new SierraLeoneTaxCalculationService(_mockLogger.Object, _mockSettingService.Object);
        }

        [Fact]
        public void CalculateIncomeTax_CorporateRate_ShouldReturn25Percent()
        {
            // Arrange
            decimal taxableIncome = 1_000_000m; // 1M SLE
            var category = TaxpayerCategory.Large;
            bool isIndividual = false;

            // Act
            var result = _service.CalculateIncomeTax(taxableIncome, category, isIndividual);

            // Assert
            result.Should().Be(250_000m); // 25% of 1M
        }

        [Theory]
        [InlineData(500_000, 0)] // Below first threshold - 0%
        [InlineData(600_000, 0)] // At first threshold - 0%
        [InlineData(1_000_000, 60_000)] // 600k at 0% + 400k at 15% = 60k
        [InlineData(1_200_000, 90_000)] // 600k at 0% + 600k at 15% = 90k
        [InlineData(1_800_000, 210_000)] // 600k at 0% + 600k at 15% + 600k at 20% = 210k
        [InlineData(2_400_000, 360_000)] // All brackets: 0 + 90k + 120k + 150k = 360k
        [InlineData(3_000_000, 540_000)] // All brackets + 600k at 30% = 360k + 180k = 540k
        public void CalculateIncomeTax_IndividualProgressive_ShouldCalculateCorrectly(decimal income, decimal expectedTax)
        {
            // Act
            var result = _service.CalculateIncomeTax(income, TaxpayerCategory.Large, isIndividual: true);

            // Assert
            result.Should().Be(expectedTax);
        }

        [Fact]
        public void CalculateGST_StandardRate_ShouldReturn15Percent()
        {
            // Arrange
            decimal taxableAmount = 1_000_000m;

            // Act
            var result = _service.CalculateGST(taxableAmount);

            // Assert
            result.Should().Be(150_000m); // 15% of 1M
        }

        [Fact]
        public void CalculateGST_ExemptItems_ShouldReturnZero()
        {
            // Arrange
            decimal taxableAmount = 1_000_000m;

            // Act
            var result = _service.CalculateGST(taxableAmount, "exempt");

            // Assert
            result.Should().Be(0);
        }

        [Theory]
        [InlineData(WithholdingTaxType.Dividends, 1_000_000, 150_000)] // 15%
        [InlineData(WithholdingTaxType.ManagementFees, 1_000_000, 150_000)] // 15%
        [InlineData(WithholdingTaxType.ProfessionalFees, 1_000_000, 150_000)] // 15%
        [InlineData(WithholdingTaxType.Rent, 1_000_000, 100_000)] // 10%
        [InlineData(WithholdingTaxType.Commissions, 1_000_000, 50_000)] // 5%
        public void CalculateWithholdingTax_DifferentTypes_ShouldCalculateCorrectly(
            WithholdingTaxType type, decimal amount, decimal expectedTax)
        {
            // Act
            var result = _service.CalculateWithholdingTax(amount, type);

            // Assert
            result.Should().Be(expectedTax);
        }

        [Fact]
        public void CalculatePAYE_ShouldUseProgressiveRates()
        {
            // Arrange
            decimal grossSalary = 1_200_000m; // 1.2M SLE

            // Act
            var result = _service.CalculatePAYE(grossSalary);

            // Assert
            // Should be same as individual income tax: 600k at 0% + 600k at 15% = 90k
            result.Should().Be(90_000m);
        }

        [Theory]
        [InlineData(PenaltyType.LateFilingPenalty, 1_000_000, 30, 50_000)] // 5% or minimum 50k
        [InlineData(PenaltyType.LatePaymentPenalty, 1_000_000, 20, 50_000)] // 5% for first 30 days
        [InlineData(PenaltyType.LatePaymentPenalty, 1_000_000, 45, 100_000)] // 10% for 31-60 days
        [InlineData(PenaltyType.LatePaymentPenalty, 1_000_000, 90, 150_000)] // 15% for over 60 days
        [InlineData(PenaltyType.UnderDeclarationPenalty, 1_000_000, 0, 200_000)] // 20%
        public void CalculatePenalty_DifferentTypes_ShouldCalculateCorrectly(
            PenaltyType type, decimal taxAmount, int daysLate, decimal expectedPenalty)
        {
            // Act
            var result = _service.CalculatePenalty(taxAmount, daysLate, type);

            // Assert
            result.Should().Be(expectedPenalty);
        }

        [Theory]
        [InlineData(1_000_000, 30, 0.15, 12_329)] // ~12,329 SLE interest for 30 days at 15% annual
        [InlineData(1_000_000, 365, 0.15, 150_000)] // 150k SLE interest for 1 year at 15% annual
        [InlineData(1_000_000, 0, 0.15, 0)] // No interest for 0 days
        public void CalculateInterest_ShouldCalculateCorrectly(
            decimal principal, int daysLate, decimal annualRate, decimal expectedInterest)
        {
            // Act
            var result = _service.CalculateInterest(principal, daysLate, annualRate);

            // Assert
            result.Should().BeApproximately(expectedInterest, 1); // Allow 1 SLE difference for rounding
        }

        [Fact]
        public void CalculateMinimumTax_ShouldReturnHalfPercentOfTurnover()
        {
            // Arrange
            decimal annualTurnover = 100_000_000m; // 100M SLE

            // Act
            var result = _service.CalculateMinimumTax(annualTurnover);

            // Assert
            result.Should().Be(500_000m); // 0.5% of 100M
        }

        [Fact]
        public void GetApplicableTax_ShouldReturnHigherOfCalculatedOrMinimum()
        {
            // Arrange
            decimal calculatedTax = 300_000m;
            decimal minimumTax = 500_000m;

            // Act
            var result = _service.GetApplicableTax(calculatedTax, minimumTax);

            // Assert
            result.Should().Be(500_000m); // Higher minimum tax applies
        }

        [Fact]
        public void CalculateTotalTaxLiability_WithLateFiling_ShouldIncludePenaltiesAndInterest()
        {
            // Arrange
            decimal taxableAmount = 1_000_000m;
            var taxType = TaxType.IncomeTax;
            var category = TaxpayerCategory.Large;
            var dueDate = DateTime.UtcNow.AddDays(-45); // 45 days late
            bool isIndividual = false;

            // Act
            var result = _service.CalculateTotalTaxLiability(
                taxableAmount, taxType, category, dueDate, 0, isIndividual);

            // Assert
            result.BaseTax.Should().Be(250_000m); // 25% corporate rate
            result.Penalty.Should().Be(100_000m); // 10% penalty for 31-60 days late
            result.Interest.Should().BeGreaterThan(0); // Should have interest
            result.TotalTaxLiability.Should().Be(result.BaseTax + result.Penalty + result.Interest);
        }

        [Fact]
        public void CalculateTotalTaxLiability_OnTime_ShouldNotIncludePenaltiesOrInterest()
        {
            // Arrange
            decimal taxableAmount = 1_000_000m;
            var taxType = TaxType.IncomeTax;
            var category = TaxpayerCategory.Large;
            var dueDate = DateTime.UtcNow.AddDays(30); // Future due date
            bool isIndividual = false;

            // Act
            var result = _service.CalculateTotalTaxLiability(
                taxableAmount, taxType, category, dueDate, 0, isIndividual);

            // Assert
            result.BaseTax.Should().Be(250_000m); // 25% corporate rate
            result.Penalty.Should().Be(0);
            result.Interest.Should().Be(0);
            result.TotalTaxLiability.Should().Be(result.BaseTax);
        }

        [Fact]
        public void CalculateTotalTaxLiability_WithMinimumTax_ShouldApplyMinimumTax()
        {
            // Arrange
            decimal taxableAmount = 1_000_000m; // Low taxable income
            decimal annualTurnover = 100_000_000m; // High turnover
            var taxType = TaxType.IncomeTax;
            var category = TaxpayerCategory.Large;
            var dueDate = DateTime.UtcNow.AddDays(30);
            bool isIndividual = false;

            // Act
            var result = _service.CalculateTotalTaxLiability(
                taxableAmount, taxType, category, dueDate, annualTurnover, isIndividual);

            // Assert
            result.BaseTax.Should().Be(500_000m); // Should use minimum tax
            result.MinimumTax.Should().Be(500_000m);
            result.TotalTaxLiability.Should().Be(500_000m);
        }
    }
}