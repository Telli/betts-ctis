using System.Linq;
using System.Threading.Tasks;
using BettsTax.Core.Services;
using BettsTax.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BettsTax.Web.Tests.Controllers
{
    public class FinanceAct2025ControllerTests
    {
        private readonly Mock<IInvestmentIncentiveCalculationService> _investmentServiceMock;
        private readonly Mock<ILogger<FinanceAct2025Controller>> _loggerMock;
        private readonly FinanceAct2025Controller _controller;

        public FinanceAct2025ControllerTests()
        {
            _investmentServiceMock = new Mock<IInvestmentIncentiveCalculationService>();
            _loggerMock = new Mock<ILogger<FinanceAct2025Controller>>();
            _controller = new FinanceAct2025Controller(_investmentServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void CalculateInvestmentIncentives_WithValidRequest_ShouldReturnOk()
        {
            // Arrange
            var request = new InvestmentIncentiveRequest
            {
                BusinessName = "Test Corp",
                InvestmentAmount = 10_000_000,
                EmployeeCount = 150,
                LocalOwnershipPercentage = 25
            };

            var expectedResult = new InvestmentIncentiveResult
            {
                BusinessName = "Test Corp",
                InvestmentAmount = 10_000_000,
                EmployeeCount = 150,
                LocalOwnershipPercentage = 25,
                BusinessSector = BusinessSector.Manufacturing,
                EmploymentBasedExemption = new EmploymentBasedExemption
                {
                    IsEligible = true,
                    ExemptionYears = 10,
                    ExemptionType = "10-Year Corporate Tax Exemption",
                    Requirements = "150+ employees, $7.5M+ investment, 20%+ local ownership",
                    EstimatedAnnualSavings = 500_000
                },
                TotalEstimatedAnnualSavings = 500_000,
                SavingsAsPercentageOfRevenue = 15.5m,
                CalculationDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                FinanceActVersion = "Finance Act 2025"
            };

            _investmentServiceMock.Setup(s => s.CalculateInvestmentIncentives(It.IsAny<InvestmentIncentiveRequest>()))
                .Returns(expectedResult);

            // Act
            var result = _controller.CalculateInvestmentIncentives(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedResult = okResult.Value.Should().BeOfType<InvestmentIncentiveResult>().Subject;
            returnedResult.BusinessName.Should().Be("Test Corp");
            returnedResult.EmploymentBasedExemption.IsEligible.Should().BeTrue();
            returnedResult.EmploymentBasedExemption.ExemptionYears.Should().Be(10);
            
            _investmentServiceMock.Verify(s => s.CalculateInvestmentIncentives(It.IsAny<InvestmentIncentiveRequest>()), Times.Once);
        }

        [Fact]
        public void CalculateInvestmentIncentives_WithException_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new InvestmentIncentiveRequest
            {
                BusinessName = "Test Corp"
            };

            _investmentServiceMock.Setup(s => s.CalculateInvestmentIncentives(It.IsAny<InvestmentIncentiveRequest>()))
                .Throws(new System.Exception("Service error"));

            // Act
            var result = _controller.CalculateInvestmentIncentives(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            dynamic errorResponse = badRequestResult.Value;
            ((string)errorResponse.Error).Should().Be("Failed to calculate investment incentives");
        }

        [Fact]
        public void CheckEmploymentExemption_WithEligibleBusiness_ShouldReturnOk()
        {
            // Arrange
            var request = new EmploymentExemptionCheckRequest
            {
                BusinessName = "Manufacturing Corp",
                InvestmentAmount = 8_000_000,
                EmployeeCount = 120,
                LocalOwnershipPercentage = 30,
                EstimatedCorporateTax = 300_000
            };

            var expectedResult = new InvestmentIncentiveResult
            {
                BusinessName = "Manufacturing Corp",
                EmploymentBasedExemption = new EmploymentBasedExemption
                {
                    IsEligible = true,
                    ExemptionYears = 5,
                    ExemptionType = "5-Year Corporate Tax Exemption",
                    EstimatedAnnualSavings = 300_000
                }
            };

            _investmentServiceMock.Setup(s => s.CalculateInvestmentIncentives(It.IsAny<InvestmentIncentiveRequest>()))
                .Returns(expectedResult);

            // Act
            var result = _controller.CheckEmploymentExemption(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((string)response.BusinessName).Should().Be("Manufacturing Corp");
            ((bool)response.IsEligible).Should().BeTrue();
            ((decimal)response.EstimatedAnnualSavings).Should().Be(300_000);
        }

        [Fact]
        public void CalculateAgribusinessExemption_WithQualifyingFarm_ShouldReturnOk()
        {
            // Arrange
            var request = new AgribusinessExemptionRequest
            {
                BusinessName = "Sierra Farms Ltd",
                InvestmentAmount = 2_000_000,
                CultivatedLandHectares = 200,
                LivestockCount = 500,
                MachineryImportValue = 500_000,
                EstimatedCorporateTax = 150_000
            };

            var expectedResult = new InvestmentIncentiveResult
            {
                BusinessName = "Sierra Farms Ltd",
                AgribusinessExemption = new AgribusinessExemption
                {
                    IsEligible = true,
                    ExemptionType = "Large-scale Agricultural Operation Exemption",
                    Requirements = "200+ hectares cultivated land or 100+ livestock",
                    EstimatedAnnualSavings = 150_000,
                    QualifyingActivities = new[] { "Large-scale cultivation", "Livestock farming", "Farm machinery import" }
                }
            };

            _investmentServiceMock.Setup(s => s.CalculateInvestmentIncentives(It.IsAny<InvestmentIncentiveRequest>()))
                .Returns(expectedResult);

            // Act
            var result = _controller.CalculateAgribusinessExemption(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((string)response.BusinessName).Should().Be("Sierra Farms Ltd");
            ((bool)response.IsEligible).Should().BeTrue();
            ((decimal)response.EstimatedAnnualSavings).Should().Be(150_000);
        }

        [Fact]
        public void CalculateRenewableEnergyIncentives_WithQualifyingProject_ShouldReturnOk()
        {
            // Arrange
            var request = new RenewableEnergyIncentiveRequest
            {
                BusinessName = "Green Energy Solutions",
                InvestmentAmount = 1_000_000,
                EmployeeCount = 75,
                EquipmentValue = 800_000,
                EstimatedCorporateTax = 200_000
            };

            var expectedResult = new InvestmentIncentiveResult
            {
                BusinessName = "Green Energy Solutions",
                RenewableEnergyExemption = new RenewableEnergyExemption
                {
                    IsEligible = true,
                    ExemptionType = "Renewable Energy Investment Incentive",
                    Requirements = "Minimum $500K investment in renewable energy equipment, 50+ employees",
                    EstimatedAnnualSavings = 100_000,
                    QualifyingEquipment = new[] { "Solar panels", "Wind turbines", "Energy storage systems" }
                }
            };

            _investmentServiceMock.Setup(s => s.CalculateInvestmentIncentives(It.IsAny<InvestmentIncentiveRequest>()))
                .Returns(expectedResult);

            // Act
            var result = _controller.CalculateRenewableEnergyIncentives(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((string)response.BusinessName).Should().Be("Green Energy Solutions");
            ((bool)response.IsEligible).Should().BeTrue();
            ((decimal)response.EstimatedAnnualSavings).Should().Be(100_000);
        }

        [Fact]
        public void CalculateDutyFreeImport_WithNewBusiness_ShouldReturnOk()
        {
            // Arrange
            var request = new DutyFreeImportRequest
            {
                BusinessName = "New Manufacturing Co",
                InvestmentAmount = 12_000_000,
                IsNewBusiness = true,
                MachineryImportValue = 3_000_000
            };

            var expectedResult = new InvestmentIncentiveResult
            {
                BusinessName = "New Manufacturing Co",
                DutyFreeImportProvisions = new[]
                {
                    new DutyFreeImportProvision
                    {
                        Type = "New Business Provision",
                        DurationYears = 3,
                        Requirements = "New business with minimum $10M investment",
                        EstimatedSavings = 450_000,
                        QualifyingItems = new[] { "Machinery", "Equipment", "Raw materials" }
                    }
                }
            };

            _investmentServiceMock.Setup(s => s.CalculateInvestmentIncentives(It.IsAny<InvestmentIncentiveRequest>()))
                .Returns(expectedResult);

            // Act
            var result = _controller.CalculateDutyFreeImport(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((string)response.BusinessName).Should().Be("New Manufacturing Co");
            ((bool)response.IsEligible).Should().BeTrue();
            ((decimal)response.TotalEstimatedSavings).Should().Be(450_000);
        }

        [Fact]
        public void CalculateRAndDDeduction_WithQualifyingExpenses_ShouldReturnOk()
        {
            // Arrange
            var request = new RAndDDeductionRequest
            {
                BusinessName = "Tech Innovation Ltd",
                RAndDExpenses = 500_000
            };

            var expectedResult = new InvestmentIncentiveResult
            {
                BusinessName = "Tech Innovation Ltd",
                RAndDDeduction = new RAndDDeduction
                {
                    IsEligible = true,
                    DeductionRate = 125,
                    RAndDExpenses = 500_000,
                    ExtraDeductionAmount = 125_000,
                    EstimatedTaxSavings = 31_250,
                    QualifyingExpenses = new[] { "Research activities", "Development projects", "Training programs" }
                }
            };

            _investmentServiceMock.Setup(s => s.CalculateInvestmentIncentives(It.IsAny<InvestmentIncentiveRequest>()))
                .Returns(expectedResult);

            // Act
            var result = _controller.CalculateRAndDDeduction(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((string)response.BusinessName).Should().Be("Tech Innovation Ltd");
            ((bool)response.IsEligible).Should().BeTrue();
            ((decimal)response.EstimatedTaxSavings).Should().Be(31_250);
        }

        [Fact]
        public void GetIncentivesSummary_ShouldReturnOkWithSummary()
        {
            // Act
            var result = _controller.GetIncentivesSummary();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic summary = okResult.Value;
            ((string)summary.FinanceActVersion).Should().Be("Finance Act 2025");
            ((string)summary.EffectiveDate).Should().Be("January 16, 2025");
            
            // Verify structure exists
            summary.InvestmentIncentives.Should().NotBeNull();
            summary.InvestmentIncentives.EmploymentBasedExemptions.Should().NotBeNull();
            summary.InvestmentIncentives.AgribusinessExemptions.Should().NotBeNull();
            summary.InvestmentIncentives.RenewableEnergyIncentives.Should().NotBeNull();
            summary.InvestmentIncentives.DutyFreeImports.Should().NotBeNull();
            summary.InvestmentIncentives.RAndDDeductions.Should().NotBeNull();
            summary.Notes.Should().NotBeNull();
        }

        [Fact]
        public void CheckEmploymentExemption_WithNonEligibleBusiness_ShouldReturnIneligible()
        {
            // Arrange
            var request = new EmploymentExemptionCheckRequest
            {
                BusinessName = "Small Business",
                InvestmentAmount = 1_000_000,
                EmployeeCount = 20,
                LocalOwnershipPercentage = 10,
                EstimatedCorporateTax = 50_000
            };

            var expectedResult = new InvestmentIncentiveResult
            {
                BusinessName = "Small Business",
                EmploymentBasedExemption = new EmploymentBasedExemption
                {
                    IsEligible = false,
                    Reason = "Does not meet minimum requirements: 100+ employees, $5M+ investment, 20%+ local ownership"
                }
            };

            _investmentServiceMock.Setup(s => s.CalculateInvestmentIncentives(It.IsAny<InvestmentIncentiveRequest>()))
                .Returns(expectedResult);

            // Act
            var result = _controller.CheckEmploymentExemption(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((string)response.BusinessName).Should().Be("Small Business");
            ((bool)response.IsEligible).Should().BeFalse();
            ((decimal)response.EstimatedAnnualSavings).Should().Be(0);
        }

        [Fact]
        public void CalculateAgribusinessExemption_WithException_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new AgribusinessExemptionRequest
            {
                BusinessName = "Test Farm"
            };

            _investmentServiceMock.Setup(s => s.CalculateInvestmentIncentives(It.IsAny<InvestmentIncentiveRequest>()))
                .Throws(new System.Exception("Service error"));

            // Act
            var result = _controller.CalculateAgribusinessExemption(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            dynamic errorResponse = badRequestResult.Value;
            ((string)errorResponse.Error).Should().Be("Failed to calculate agribusiness exemption");
        }

        [Fact]
        public void CalculateRenewableEnergyIncentives_WithException_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new RenewableEnergyIncentiveRequest
            {
                BusinessName = "Test Energy Co"
            };

            _investmentServiceMock.Setup(s => s.CalculateInvestmentIncentives(It.IsAny<InvestmentIncentiveRequest>()))
                .Throws(new System.Exception("Service error"));

            // Act
            var result = _controller.CalculateRenewableEnergyIncentives(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            dynamic errorResponse = badRequestResult.Value;
            ((string)errorResponse.Error).Should().Be("Failed to calculate renewable energy incentives");
        }

        [Fact]
        public void CalculateDutyFreeImport_WithException_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new DutyFreeImportRequest
            {
                BusinessName = "Test Import Co"
            };

            _investmentServiceMock.Setup(s => s.CalculateInvestmentIncentives(It.IsAny<InvestmentIncentiveRequest>()))
                .Throws(new System.Exception("Service error"));

            // Act
            var result = _controller.CalculateDutyFreeImport(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            dynamic errorResponse = badRequestResult.Value;
            ((string)errorResponse.Error).Should().Be("Failed to calculate duty-free import eligibility");
        }

        [Fact]
        public void CalculateRAndDDeduction_WithException_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new RAndDDeductionRequest
            {
                BusinessName = "Test R&D Co"
            };

            _investmentServiceMock.Setup(s => s.CalculateInvestmentIncentives(It.IsAny<InvestmentIncentiveRequest>()))
                .Throws(new System.Exception("Service error"));

            // Act
            var result = _controller.CalculateRAndDDeduction(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            dynamic errorResponse = badRequestResult.Value;
            ((string)errorResponse.Error).Should().Be("Failed to calculate R&D deduction");
        }
    }
}