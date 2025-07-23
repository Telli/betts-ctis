using BettsTax.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BettsTax.Core.Tests.Services
{
    public class InvestmentIncentiveCalculationServiceTests
    {
        private readonly InvestmentIncentiveCalculationService _service;
        private readonly Mock<ILogger<InvestmentIncentiveCalculationService>> _mockLogger;

        public InvestmentIncentiveCalculationServiceTests()
        {
            _mockLogger = new Mock<ILogger<InvestmentIncentiveCalculationService>>();
            _service = new InvestmentIncentiveCalculationService(_mockLogger.Object);
        }

        [Fact]
        public void CalculateInvestmentIncentives_ShouldReturnBasicInformation()
        {
            // Arrange
            var request = new InvestmentIncentiveRequest
            {
                BusinessName = "Test Company",
                InvestmentAmount = 1_000_000,
                EmployeeCount = 50,
                LocalOwnershipPercentage = 30,
                BusinessSector = BusinessSector.Manufacturing
            };

            // Act
            var result = _service.CalculateInvestmentIncentives(request);

            // Assert
            result.BusinessName.Should().Be("Test Company");
            result.InvestmentAmount.Should().Be(1_000_000);
            result.EmployeeCount.Should().Be(50);
            result.LocalOwnershipPercentage.Should().Be(30);
            result.BusinessSector.Should().Be(BusinessSector.Manufacturing);
        }

        [Theory]
        [InlineData(100, 5_000_000, 25, true, 5)] // Meets 5-year exemption criteria
        [InlineData(150, 7_500_000, 25, true, 10)] // Meets 10-year exemption criteria
        [InlineData(80, 5_000_000, 25, false, 0)] // Not enough employees
        [InlineData(100, 4_000_000, 25, false, 0)] // Not enough investment
        [InlineData(100, 5_000_000, 15, false, 0)] // Not enough local ownership
        public void CheckEmploymentBasedExemptions_ShouldCalculateCorrectly(
            int employees, decimal investment, decimal localOwnership, bool expectedEligible, int expectedYears)
        {
            // Arrange
            var request = new InvestmentIncentiveRequest
            {
                BusinessName = "Test Company",
                EmployeeCount = employees,
                InvestmentAmount = investment,
                LocalOwnershipPercentage = localOwnership,
                EstimatedCorporateTax = 100_000
            };

            // Act
            var result = _service.CalculateInvestmentIncentives(request);

            // Assert
            result.EmploymentBasedExemption.Should().NotBeNull();
            result.EmploymentBasedExemption!.IsEligible.Should().Be(expectedEligible);
            
            if (expectedEligible)
            {
                result.EmploymentBasedExemption.ExemptionYears.Should().Be(expectedYears);
                result.EmploymentBasedExemption.EstimatedAnnualSavings.Should().Be(100_000);
            }
        }

        [Theory]
        [InlineData(BusinessSector.Agriculture, 100, 0, 200_000, true)] // Large land cultivation
        [InlineData(BusinessSector.Agriculture, 0, 150, 200_000, true)] // Large livestock
        [InlineData(BusinessSector.Agriculture, 30, 50, 200_000, false)] // Small scale
        [InlineData(BusinessSector.Manufacturing, 100, 0, 200_000, false)] // Wrong sector
        public void CheckAgribusinessExemptions_ShouldCalculateCorrectly(
            BusinessSector sector, decimal landHectares, int livestock, decimal corporateTax, bool expectedEligible)
        {
            // Arrange
            var request = new InvestmentIncentiveRequest
            {
                BusinessSector = sector,
                CultivatedLandHectares = landHectares,
                LivestockCount = livestock,
                EstimatedCorporateTax = corporateTax,
                MachineryImportValue = 100_000
            };

            // Act
            var result = _service.CalculateInvestmentIncentives(request);

            // Assert
            result.AgribusinessExemption.Should().NotBeNull();
            result.AgribusinessExemption!.IsEligible.Should().Be(expectedEligible);
            
            if (expectedEligible)
            {
                result.AgribusinessExemption.EstimatedAnnualSavings.Should().BeGreaterThan(0);
                result.AgribusinessExemption.QualifyingActivities.Should().NotBeEmpty();
            }
        }

        [Theory]
        [InlineData(BusinessSector.RenewableEnergy, 500_000, 50, true)] // Meets requirements
        [InlineData(BusinessSector.RenewableEnergy, 400_000, 50, false)] // Low investment
        [InlineData(BusinessSector.RenewableEnergy, 500_000, 40, false)] // Low employment
        [InlineData(BusinessSector.Manufacturing, 500_000, 50, false)] // Wrong sector
        public void CheckRenewableEnergyExemptions_ShouldCalculateCorrectly(
            BusinessSector sector, decimal investment, int employees, bool expectedEligible)
        {
            // Arrange
            var request = new InvestmentIncentiveRequest
            {
                BusinessSector = sector,
                InvestmentAmount = investment,
                EmployeeCount = employees,
                RenewableEnergyEquipmentValue = 200_000
            };

            // Act
            var result = _service.CalculateInvestmentIncentives(request);

            // Assert
            result.RenewableEnergyExemption.Should().NotBeNull();
            result.RenewableEnergyExemption!.IsEligible.Should().Be(expectedEligible);
            
            if (expectedEligible)
            {
                result.RenewableEnergyExemption.QualifyingEquipment.Should().NotBeEmpty();
            }
        }

        [Theory]
        [InlineData(true, 10_000_000, 1)] // New business with sufficient investment
        [InlineData(false, 5_000_000, 1)] // Existing business expanding
        [InlineData(true, 5_000_000, 0)] // New business with insufficient investment
        [InlineData(false, 3_000_000, 0)] // Existing business with insufficient investment
        public void CheckDutyFreeImportEligibility_ShouldCalculateCorrectly(
            bool isNewBusiness, decimal investment, int expectedProvisions)
        {
            // Arrange
            var request = new InvestmentIncentiveRequest
            {
                IsNewBusiness = isNewBusiness,
                InvestmentAmount = investment,
                MachineryImportValue = 1_000_000
            };

            // Act
            var result = _service.CalculateInvestmentIncentives(request);

            // Assert
            result.DutyFreeImportProvisions.Should().HaveCount(expectedProvisions);
            
            if (expectedProvisions > 0)
            {
                result.DutyFreeImportProvisions.First().DurationYears.Should().Be(3);
                result.DutyFreeImportProvisions.First().EstimatedSavings.Should().BeGreaterThan(0);
            }
        }

        [Theory]
        [InlineData(100_000, true, 25_000, 6_250)] // Valid R&D expenses
        [InlineData(0, false, 0, 0)] // No R&D expenses
        public void CheckRAndDDeductions_ShouldCalculateCorrectly(
            decimal rdExpenses, bool expectedEligible, decimal expectedExtraDeduction, decimal expectedTaxSavings)
        {
            // Arrange
            var request = new InvestmentIncentiveRequest
            {
                RAndDExpenses = rdExpenses
            };

            // Act
            var result = _service.CalculateInvestmentIncentives(request);

            // Assert
            result.RAndDDeduction.Should().NotBeNull();
            result.RAndDDeduction!.IsEligible.Should().Be(expectedEligible);
            
            if (expectedEligible)
            {
                result.RAndDDeduction.DeductionRate.Should().Be(125);
                result.RAndDDeduction.ExtraDeductionAmount.Should().Be(expectedExtraDeduction);
                result.RAndDDeduction.EstimatedTaxSavings.Should().Be(expectedTaxSavings);
                result.RAndDDeduction.QualifyingExpenses.Should().NotBeEmpty();
            }
        }

        [Fact]
        public void CalculateInvestmentIncentives_ComprehensiveCase_ShouldCalculateAllIncentives()
        {
            // Arrange - A renewable energy company that qualifies for multiple incentives
            var request = new InvestmentIncentiveRequest
            {
                BusinessName = "Green Energy Corp",
                BusinessSector = BusinessSector.RenewableEnergy,
                InvestmentAmount = 8_000_000,
                EmployeeCount = 160,
                LocalOwnershipPercentage = 30,
                IsNewBusiness = true,
                AnnualRevenue = 5_000_000,
                EstimatedCorporateTax = 500_000,
                RenewableEnergyEquipmentValue = 2_000_000,
                MachineryImportValue = 1_000_000,
                RAndDExpenses = 200_000
            };

            // Act
            var result = _service.CalculateInvestmentIncentives(request);

            // Assert
            result.BusinessName.Should().Be("Green Energy Corp");
            
            // Should qualify for employment-based exemption (10-year)
            result.EmploymentBasedExemption!.IsEligible.Should().BeTrue();
            result.EmploymentBasedExemption.ExemptionYears.Should().Be(10);
            
            // Should qualify for renewable energy exemptions
            result.RenewableEnergyExemption!.IsEligible.Should().BeTrue();
            
            // Should qualify for duty-free imports (new business)
            result.DutyFreeImportProvisions.Should().HaveCountGreaterThan(0);
            
            // Should qualify for R&D deductions
            result.RAndDDeduction!.IsEligible.Should().BeTrue();
            
            // Should have total savings calculated
            result.TotalEstimatedAnnualSavings.Should().BeGreaterThan(0);
            result.SavingsAsPercentageOfRevenue.Should().BeGreaterThan(0);
            
            // Should have proper metadata
            result.FinanceActVersion.Should().Be("Finance Act 2025");
            result.CalculationDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void CalculateInvestmentIncentives_AgricultureCase_ShouldCalculateAgricultureSpecificIncentives()
        {
            // Arrange - A large-scale agricultural operation
            var request = new InvestmentIncentiveRequest
            {
                BusinessName = "Sierra Farms Ltd",
                BusinessSector = BusinessSector.Agriculture,
                InvestmentAmount = 2_000_000,
                EmployeeCount = 80,
                LocalOwnershipPercentage = 25,
                CultivatedLandHectares = 200,
                LivestockCount = 500,
                MachineryImportValue = 500_000,
                EstimatedCorporateTax = 200_000,
                AnnualRevenue = 3_000_000
            };

            // Act
            var result = _service.CalculateInvestmentIncentives(request);

            // Assert
            result.BusinessName.Should().Be("Sierra Farms Ltd");
            
            // Should NOT qualify for employment-based exemption (insufficient employees/investment)
            result.EmploymentBasedExemption!.IsEligible.Should().BeFalse();
            
            // Should qualify for agribusiness exemptions
            result.AgribusinessExemption!.IsEligible.Should().BeTrue();
            result.AgribusinessExemption.EstimatedAnnualSavings.Should().BeGreaterThan(0);
            result.AgribusinessExemption.QualifyingActivities.Should().NotBeEmpty();
            
            // Should have total savings from agribusiness exemptions
            result.TotalEstimatedAnnualSavings.Should().BeGreaterThan(0);
            result.SavingsAsPercentageOfRevenue.Should().BeGreaterThan(0);
        }
    }
}