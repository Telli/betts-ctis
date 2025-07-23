using BettsTax.Core.Services;
using BettsTax.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TaxCalculationController : ControllerBase
    {
        private readonly ISierraLeoneTaxCalculationService _taxCalculationService;
        private readonly ITaxFilingService _taxFilingService;
        private readonly ILogger<TaxCalculationController> _logger;

        public TaxCalculationController(
            ISierraLeoneTaxCalculationService taxCalculationService,
            ITaxFilingService taxFilingService,
            ILogger<TaxCalculationController> logger)
        {
            _taxCalculationService = taxCalculationService;
            _taxFilingService = taxFilingService;
            _logger = logger;
        }

        /// <summary>
        /// Calculate Income Tax for individuals or corporations
        /// </summary>
        [HttpPost("income-tax")]
        public IActionResult CalculateIncomeTax([FromBody] IncomeTaxCalculationRequest request)
        {
            try
            {
                var tax = _taxCalculationService.CalculateIncomeTax(
                    request.TaxableIncome, 
                    request.TaxpayerCategory, 
                    request.IsIndividual);

                return Ok(new { TaxAmount = tax, Currency = "SLE" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating income tax");
                return BadRequest(new { Error = "Failed to calculate income tax" });
            }
        }

        /// <summary>
        /// Calculate GST (Goods and Services Tax)
        /// </summary>
        [HttpPost("gst")]
        public IActionResult CalculateGST([FromBody] GSTCalculationRequest request)
        {
            try
            {
                var gst = _taxCalculationService.CalculateGST(
                    request.TaxableAmount, 
                    request.ItemCategory);

                return Ok(new { GSTAmount = gst, Rate = "15%", Currency = "SLE" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating GST");
                return BadRequest(new { Error = "Failed to calculate GST" });
            }
        }

        /// <summary>
        /// Calculate Withholding Tax based on Finance Act 2024
        /// </summary>
        [HttpPost("withholding-tax")]
        public IActionResult CalculateWithholdingTax([FromBody] WithholdingTaxCalculationRequest request)
        {
            try
            {
                var withholdingTax = _taxCalculationService.CalculateWithholdingTax(
                    request.Amount, 
                    request.WithholdingTaxType, 
                    request.IsResident);

                return Ok(new { WithholdingTaxAmount = withholdingTax, Currency = "SLE" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating withholding tax");
                return BadRequest(new { Error = "Failed to calculate withholding tax" });
            }
        }

        /// <summary>
        /// Calculate PAYE (Pay As You Earn) tax for employees
        /// </summary>
        [HttpPost("paye")]
        public IActionResult CalculatePAYE([FromBody] PAYECalculationRequest request)
        {
            try
            {
                var paye = _taxCalculationService.CalculatePAYE(
                    request.GrossSalary, 
                    request.Allowances);

                return Ok(new { PAYEAmount = paye, Currency = "SLE" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating PAYE");
                return BadRequest(new { Error = "Failed to calculate PAYE" });
            }
        }

        /// <summary>
        /// Calculate penalties for late filing or payment
        /// </summary>
        [HttpPost("penalty")]
        public IActionResult CalculatePenalty([FromBody] PenaltyCalculationRequest request)
        {
            try
            {
                var penalty = _taxCalculationService.CalculatePenalty(
                    request.TaxAmount, 
                    request.DaysLate, 
                    request.PenaltyType);

                return Ok(new { PenaltyAmount = penalty, Currency = "SLE" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating penalty");
                return BadRequest(new { Error = "Failed to calculate penalty" });
            }
        }

        /// <summary>
        /// Calculate interest on late payments
        /// </summary>
        [HttpPost("interest")]
        public IActionResult CalculateInterest([FromBody] InterestCalculationRequest request)
        {
            try
            {
                var interest = _taxCalculationService.CalculateInterest(
                    request.PrincipalAmount, 
                    request.DaysLate, 
                    request.AnnualInterestRate);

                return Ok(new { InterestAmount = interest, Currency = "SLE" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating interest");
                return BadRequest(new { Error = "Failed to calculate interest" });
            }
        }

        /// <summary>
        /// Calculate comprehensive tax liability including penalties and interest
        /// </summary>
        [HttpPost("comprehensive/{clientId}")]
        public async Task<IActionResult> CalculateComprehensiveTaxLiability(
            int clientId, 
            [FromBody] ComprehensiveTaxCalculationRequest request)
        {
            try
            {
                var result = await _taxFilingService.CalculateComprehensiveTaxLiabilityAsync(
                    clientId,
                    request.TaxType,
                    request.TaxYear,
                    request.TaxableAmount,
                    request.DueDate,
                    request.AnnualTurnover,
                    request.IsIndividual);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating comprehensive tax liability for client {ClientId}", clientId);
                return BadRequest(new { Error = "Failed to calculate comprehensive tax liability" });
            }
        }

        /// <summary>
        /// Get current tax rates and thresholds
        /// </summary>
        [HttpGet("rates")]
        public IActionResult GetTaxRates()
        {
            var rates = new
            {
                IncomeTax = new
                {
                    Individual = new[]
                    {
                        new { Threshold = "0 - 600,000 SLE", Rate = "0%" },
                        new { Threshold = "600,001 - 1,200,000 SLE", Rate = "15%" },
                        new { Threshold = "1,200,001 - 1,800,000 SLE", Rate = "20%" },
                        new { Threshold = "1,800,001 - 2,400,000 SLE", Rate = "25%" },
                        new { Threshold = "Above 2,400,000 SLE", Rate = "30%" }
                    },
                    Corporate = "25%"
                },
                GST = "15%",
                WithholdingTax = new
                {
                    Dividends = "15%",
                    ManagementFees = "15%",
                    ProfessionalFees = "15%",
                    Rent = "10%",
                    Commissions = "5%"
                },
                MinimumTax = "0.5% of annual turnover",
                PenaltyRates = new
                {
                    LateFilingPenalty = "5% of tax due or minimum 50,000 SLE",
                    LatePaymentPenalty = new[]
                    {
                        new { Period = "1-30 days", Rate = "5%" },
                        new { Period = "31-60 days", Rate = "10%" },
                        new { Period = "Over 60 days", Rate = "15%" }
                    }
                },
                InterestRate = "15% per annum",
                FinanceActVersion = "Finance Act 2024",
                LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd")
            };

            return Ok(rates);
        }
    }

    // Request DTOs
    public class IncomeTaxCalculationRequest
    {
        public decimal TaxableIncome { get; set; }
        public TaxpayerCategory TaxpayerCategory { get; set; }
        public bool IsIndividual { get; set; }
    }

    public class GSTCalculationRequest
    {
        public decimal TaxableAmount { get; set; }
        public string ItemCategory { get; set; } = "standard";
    }

    public class WithholdingTaxCalculationRequest
    {
        public decimal Amount { get; set; }
        public WithholdingTaxType WithholdingTaxType { get; set; }
        public bool IsResident { get; set; } = true;
    }

    public class PAYECalculationRequest
    {
        public decimal GrossSalary { get; set; }
        public decimal Allowances { get; set; }
    }

    public class PenaltyCalculationRequest
    {
        public decimal TaxAmount { get; set; }
        public int DaysLate { get; set; }
        public PenaltyType PenaltyType { get; set; }
    }

    public class InterestCalculationRequest
    {
        public decimal PrincipalAmount { get; set; }
        public int DaysLate { get; set; }
        public decimal AnnualInterestRate { get; set; } = 0.15m;
    }

    public class ComprehensiveTaxCalculationRequest
    {
        public TaxType TaxType { get; set; }
        public int TaxYear { get; set; }
        public decimal TaxableAmount { get; set; }
        public DateTime DueDate { get; set; }
        public decimal AnnualTurnover { get; set; }
        public bool IsIndividual { get; set; }
    }
}