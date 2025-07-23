using System;
using System.Threading.Tasks;
using BettsTax.Core.Services;
using BettsTax.Data;
using BettsTax.Web.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BettsTax.Web.Tests.Controllers
{
    public class TaxCalculationControllerTests
    {
        private readonly Mock<ISierraLeoneTaxCalculationService> _taxCalculationServiceMock;
        private readonly Mock<ITaxFilingService> _taxFilingServiceMock;
        private readonly Mock<ILogger<TaxCalculationController>> _loggerMock;
        private readonly TaxCalculationController _controller;

        public TaxCalculationControllerTests()
        {
            _taxCalculationServiceMock = new Mock<ISierraLeoneTaxCalculationService>();
            _taxFilingServiceMock = new Mock<ITaxFilingService>();
            _loggerMock = new Mock<ILogger<TaxCalculationController>>();
            _controller = new TaxCalculationController(
                _taxCalculationServiceMock.Object,
                _taxFilingServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public void CalculateIncomeTax_WithValidIndividualRequest_ShouldReturnOk()
        {
            // Arrange
            var request = new IncomeTaxCalculationRequest
            {
                TaxableIncome = 1_500_000,
                TaxpayerCategory = TaxpayerCategory.Medium,
                IsIndividual = true
            };

            var expectedTax = 135_000m;
            _taxCalculationServiceMock.Setup(s => s.CalculateIncomeTax(
                    request.TaxableIncome,
                    request.TaxpayerCategory,
                    request.IsIndividual))
                .Returns(expectedTax);

            // Act
            var result = _controller.CalculateIncomeTax(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((decimal)response.TaxAmount).Should().Be(expectedTax);
            ((string)response.Currency).Should().Be("SLE");

            _taxCalculationServiceMock.Verify(s => s.CalculateIncomeTax(
                request.TaxableIncome,
                request.TaxpayerCategory,
                request.IsIndividual), Times.Once);
        }

        [Fact]
        public void CalculateIncomeTax_WithValidCorporateRequest_ShouldReturnOk()
        {
            // Arrange
            var request = new IncomeTaxCalculationRequest
            {
                TaxableIncome = 2_000_000,
                TaxpayerCategory = TaxpayerCategory.Large,
                IsIndividual = false
            };

            var expectedTax = 500_000m; // 25% of 2M
            _taxCalculationServiceMock.Setup(s => s.CalculateIncomeTax(
                    request.TaxableIncome,
                    request.TaxpayerCategory,
                    request.IsIndividual))
                .Returns(expectedTax);

            // Act
            var result = _controller.CalculateIncomeTax(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((decimal)response.TaxAmount).Should().Be(expectedTax);
        }

        [Fact]
        public void CalculateIncomeTax_WithException_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new IncomeTaxCalculationRequest
            {
                TaxableIncome = 1_000_000,
                TaxpayerCategory = TaxpayerCategory.Medium,
                IsIndividual = true
            };

            _taxCalculationServiceMock.Setup(s => s.CalculateIncomeTax(
                    It.IsAny<decimal>(),
                    It.IsAny<TaxpayerCategory>(),
                    It.IsAny<bool>()))
                .Throws(new Exception("Service error"));

            // Act
            var result = _controller.CalculateIncomeTax(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            dynamic errorResponse = badRequestResult.Value;
            ((string)errorResponse.Error).Should().Be("Failed to calculate income tax");
        }

        [Fact]
        public void CalculateGST_WithStandardRate_ShouldReturnOk()
        {
            // Arrange
            var request = new GSTCalculationRequest
            {
                TaxableAmount = 1_000_000,
                ItemCategory = "standard"
            };

            var expectedGST = 150_000m; // 15% of 1M
            _taxCalculationServiceMock.Setup(s => s.CalculateGST(
                    request.TaxableAmount,
                    request.ItemCategory))
                .Returns(expectedGST);

            // Act
            var result = _controller.CalculateGST(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((decimal)response.GSTAmount).Should().Be(expectedGST);
            ((string)response.Rate).Should().Be("15%");
            ((string)response.Currency).Should().Be("SLE");
        }

        [Fact]
        public void CalculateGST_WithExemptCategory_ShouldReturnZero()
        {
            // Arrange
            var request = new GSTCalculationRequest
            {
                TaxableAmount = 1_000_000,
                ItemCategory = "exempt"
            };

            var expectedGST = 0m;
            _taxCalculationServiceMock.Setup(s => s.CalculateGST(
                    request.TaxableAmount,
                    request.ItemCategory))
                .Returns(expectedGST);

            // Act
            var result = _controller.CalculateGST(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((decimal)response.GSTAmount).Should().Be(0);
        }

        [Fact]
        public void CalculateWithholdingTax_WithDividends_ShouldReturnOk()
        {
            // Arrange
            var request = new WithholdingTaxCalculationRequest
            {
                Amount = 1_000_000,
                WithholdingTaxType = WithholdingTaxType.Dividends,
                IsResident = true
            };

            var expectedTax = 150_000m; // 15% of 1M
            _taxCalculationServiceMock.Setup(s => s.CalculateWithholdingTax(
                    request.Amount,
                    request.WithholdingTaxType,
                    request.IsResident))
                .Returns(expectedTax);

            // Act
            var result = _controller.CalculateWithholdingTax(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((decimal)response.WithholdingTaxAmount).Should().Be(expectedTax);
            ((string)response.Currency).Should().Be("SLE");
        }

        [Fact]
        public void CalculatePAYE_WithSalaryAndAllowances_ShouldReturnOk()
        {
            // Arrange
            var request = new PAYECalculationRequest
            {
                GrossSalary = 800_000,
                Allowances = 200_000
            };

            var expectedPAYE = 60_000m;
            _taxCalculationServiceMock.Setup(s => s.CalculatePAYE(
                    request.GrossSalary,
                    request.Allowances))
                .Returns(expectedPAYE);

            // Act
            var result = _controller.CalculatePAYE(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((decimal)response.PAYEAmount).Should().Be(expectedPAYE);
            ((string)response.Currency).Should().Be("SLE");
        }

        [Fact]
        public void CalculatePenalty_WithLateFilingPenalty_ShouldReturnOk()
        {
            // Arrange
            var request = new PenaltyCalculationRequest
            {
                TaxAmount = 1_000_000,
                DaysLate = 45,
                PenaltyType = PenaltyType.LateFilingPenalty
            };

            var expectedPenalty = 50_000m;
            _taxCalculationServiceMock.Setup(s => s.CalculatePenalty(
                    request.TaxAmount,
                    request.DaysLate,
                    request.PenaltyType))
                .Returns(expectedPenalty);

            // Act
            var result = _controller.CalculatePenalty(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((decimal)response.PenaltyAmount).Should().Be(expectedPenalty);
            ((string)response.Currency).Should().Be("SLE");
        }

        [Fact]
        public void CalculateInterest_WithLatePayment_ShouldReturnOk()
        {
            // Arrange
            var request = new InterestCalculationRequest
            {
                PrincipalAmount = 1_000_000,
                DaysLate = 90,
                AnnualInterestRate = 0.15m
            };

            var expectedInterest = 36_986m;
            _taxCalculationServiceMock.Setup(s => s.CalculateInterest(
                    request.PrincipalAmount,
                    request.DaysLate,
                    request.AnnualInterestRate))
                .Returns(expectedInterest);

            // Act
            var result = _controller.CalculateInterest(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((decimal)response.InterestAmount).Should().Be(expectedInterest);
            ((string)response.Currency).Should().Be("SLE");
        }

        [Fact]
        public async Task CalculateComprehensiveTaxLiability_WithValidRequest_ShouldReturnOk()
        {
            // Arrange
            var clientId = 123;
            var request = new ComprehensiveTaxCalculationRequest
            {
                TaxType = TaxType.IncomeTax,
                TaxYear = 2024,
                TaxableAmount = 2_000_000,
                DueDate = DateTime.UtcNow.AddDays(-30),
                AnnualTurnover = 10_000_000,
                IsIndividual = false
            };

            var expectedResult = new TaxCalculationResult
            {
                BaseTax = 500_000,
                MinimumTax = 50_000,
                MinimumAlternateTax = 300_000,
                Penalty = 25_000,
                Interest = 12_329,
                TotalTaxLiability = 537_329,
                CalculationDate = DateTime.UtcNow,
                ApplicableTaxType = "Minimum Alternate Tax (MAT)"
            };

            _taxFilingServiceMock.Setup(s => s.CalculateComprehensiveTaxLiabilityAsync(
                    clientId,
                    request.TaxType,
                    request.TaxYear,
                    request.TaxableAmount,
                    request.DueDate,
                    request.AnnualTurnover,
                    request.IsIndividual))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CalculateComprehensiveTaxLiability(clientId, request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedResult = okResult.Value.Should().BeOfType<TaxCalculationResult>().Subject;
            returnedResult.BaseTax.Should().Be(500_000);
            returnedResult.TotalTaxLiability.Should().Be(537_329);
            returnedResult.ApplicableTaxType.Should().Be("Minimum Alternate Tax (MAT)");
        }

        [Fact]
        public async Task CalculateComprehensiveTaxLiability_WithException_ShouldReturnBadRequest()
        {
            // Arrange
            var clientId = 123;
            var request = new ComprehensiveTaxCalculationRequest
            {
                TaxType = TaxType.IncomeTax,
                TaxYear = 2024,
                TaxableAmount = 2_000_000,
                DueDate = DateTime.UtcNow,
                AnnualTurnover = 10_000_000,
                IsIndividual = false
            };

            _taxFilingServiceMock.Setup(s => s.CalculateComprehensiveTaxLiabilityAsync(
                    It.IsAny<int>(),
                    It.IsAny<TaxType>(),
                    It.IsAny<int>(),
                    It.IsAny<decimal>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<decimal>(),
                    It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.CalculateComprehensiveTaxLiability(clientId, request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            dynamic errorResponse = badRequestResult.Value;
            ((string)errorResponse.Error).Should().Be("Failed to calculate comprehensive tax liability");
        }

        [Fact]
        public void GetTaxRates_ShouldReturnOkWithCurrentRates()
        {
            // Act
            var result = _controller.GetTaxRates();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic rates = okResult.Value;
            
            // Verify structure and key rates
            rates.IncomeTax.Should().NotBeNull();
            rates.IncomeTax.Corporate.Should().Be("25%");
            rates.GST.Should().Be("15%");
            rates.WithholdingTax.Should().NotBeNull();
            rates.WithholdingTax.Dividends.Should().Be("15%");
            rates.FinanceActVersion.Should().Be("Finance Act 2024");
            rates.LastUpdated.Should().NotBeNull();
        }

        [Theory]
        [InlineData(WithholdingTaxType.Rent, 100_000, true)]
        [InlineData(WithholdingTaxType.Commissions, 200_000, false)]
        [InlineData(WithholdingTaxType.ProfessionalFees, 500_000, true)]
        public void CalculateWithholdingTax_WithDifferentTypes_ShouldCalculateCorrectly(
            WithholdingTaxType taxType, decimal amount, bool isResident)
        {
            // Arrange
            var request = new WithholdingTaxCalculationRequest
            {
                Amount = amount,
                WithholdingTaxType = taxType,
                IsResident = isResident
            };

            var expectedTax = amount * 0.15m; // Mock return value
            _taxCalculationServiceMock.Setup(s => s.CalculateWithholdingTax(
                    request.Amount,
                    request.WithholdingTaxType,
                    request.IsResident))
                .Returns(expectedTax);

            // Act
            var result = _controller.CalculateWithholdingTax(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((decimal)response.WithholdingTaxAmount).Should().Be(expectedTax);

            _taxCalculationServiceMock.Verify(s => s.CalculateWithholdingTax(
                amount, taxType, isResident), Times.Once);
        }

        [Theory]
        [InlineData(PenaltyType.LateFilingPenalty, 1_000_000, 30)]
        [InlineData(PenaltyType.LatePaymentPenalty, 500_000, 45)]
        [InlineData(PenaltyType.UnderDeclarationPenalty, 200_000, 0)]
        public void CalculatePenalty_WithDifferentPenaltyTypes_ShouldCalculateCorrectly(
            PenaltyType penaltyType, decimal taxAmount, int daysLate)
        {
            // Arrange
            var request = new PenaltyCalculationRequest
            {
                TaxAmount = taxAmount,
                DaysLate = daysLate,
                PenaltyType = penaltyType
            };

            var expectedPenalty = taxAmount * 0.05m; // Mock return value
            _taxCalculationServiceMock.Setup(s => s.CalculatePenalty(
                    request.TaxAmount,
                    request.DaysLate,
                    request.PenaltyType))
                .Returns(expectedPenalty);

            // Act
            var result = _controller.CalculatePenalty(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
            ((decimal)response.PenaltyAmount).Should().Be(expectedPenalty);

            _taxCalculationServiceMock.Verify(s => s.CalculatePenalty(
                taxAmount, daysLate, penaltyType), Times.Once);
        }

        [Fact]
        public void CalculateGST_WithException_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new GSTCalculationRequest
            {
                TaxableAmount = 1_000_000,
                ItemCategory = "standard"
            };

            _taxCalculationServiceMock.Setup(s => s.CalculateGST(
                    It.IsAny<decimal>(),
                    It.IsAny<string>()))
                .Throws(new Exception("GST calculation error"));

            // Act
            var result = _controller.CalculateGST(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            dynamic errorResponse = badRequestResult.Value;
            ((string)errorResponse.Error).Should().Be("Failed to calculate GST");
        }

        [Fact]
        public void CalculatePAYE_WithException_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new PAYECalculationRequest
            {
                GrossSalary = 800_000,
                Allowances = 200_000
            };

            _taxCalculationServiceMock.Setup(s => s.CalculatePAYE(
                    It.IsAny<decimal>(),
                    It.IsAny<decimal>()))
                .Throws(new Exception("PAYE calculation error"));

            // Act
            var result = _controller.CalculatePAYE(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            dynamic errorResponse = badRequestResult.Value;
            ((string)errorResponse.Error).Should().Be("Failed to calculate PAYE");
        }

        [Fact]
        public void CalculateInterest_WithException_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new InterestCalculationRequest
            {
                PrincipalAmount = 1_000_000,
                DaysLate = 90
            };

            _taxCalculationServiceMock.Setup(s => s.CalculateInterest(
                    It.IsAny<decimal>(),
                    It.IsAny<int>(),
                    It.IsAny<decimal>()))
                .Throws(new Exception("Interest calculation error"));

            // Act
            var result = _controller.CalculateInterest(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            dynamic errorResponse = badRequestResult.Value;
            ((string)errorResponse.Error).Should().Be("Failed to calculate interest");
        }
    }
}