using BettsTax.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FinanceAct2025Controller : ControllerBase
    {
        private readonly IInvestmentIncentiveCalculationService _investmentIncentiveService;
        private readonly ILogger<FinanceAct2025Controller> _logger;

        public FinanceAct2025Controller(
            IInvestmentIncentiveCalculationService investmentIncentiveService,
            ILogger<FinanceAct2025Controller> logger)
        {
            _investmentIncentiveService = investmentIncentiveService;
            _logger = logger;
        }

        /// <summary>
        /// Calculate comprehensive investment incentives based on Finance Act 2025
        /// </summary>
        [HttpPost("investment-incentives")]
        public IActionResult CalculateInvestmentIncentives([FromBody] InvestmentIncentiveRequest request)
        {
            try
            {
                var result = _investmentIncentiveService.CalculateInvestmentIncentives(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating investment incentives for {BusinessName}", request.BusinessName);
                return BadRequest(new { Error = "Failed to calculate investment incentives" });
            }
        }

        /// <summary>
        /// Check eligibility for employment-based tax exemptions
        /// </summary>
        [HttpPost("employment-exemption-check")]
        public IActionResult CheckEmploymentExemption([FromBody] EmploymentExemptionCheckRequest request)
        {
            try
            {
                var incentiveRequest = new InvestmentIncentiveRequest
                {
                    BusinessName = request.BusinessName,
                    InvestmentAmount = request.InvestmentAmount,
                    EmployeeCount = request.EmployeeCount,
                    LocalOwnershipPercentage = request.LocalOwnershipPercentage,
                    EstimatedCorporateTax = request.EstimatedCorporateTax
                };

                var result = _investmentIncentiveService.CalculateInvestmentIncentives(incentiveRequest);

                return Ok(new
                {
                    BusinessName = request.BusinessName,
                    IsEligible = result.EmploymentBasedExemption?.IsEligible ?? false,
                    ExemptionDetails = result.EmploymentBasedExemption,
                    EstimatedAnnualSavings = result.EmploymentBasedExemption?.EstimatedAnnualSavings ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking employment exemption eligibility");
                return BadRequest(new { Error = "Failed to check employment exemption eligibility" });
            }
        }

        /// <summary>
        /// Calculate agribusiness tax exemptions
        /// </summary>
        [HttpPost("agribusiness-exemption")]
        public IActionResult CalculateAgribusinessExemption([FromBody] AgribusinessExemptionRequest request)
        {
            try
            {
                var incentiveRequest = new InvestmentIncentiveRequest
                {
                    BusinessName = request.BusinessName,
                    BusinessSector = BusinessSector.Agriculture,
                    InvestmentAmount = request.InvestmentAmount,
                    CultivatedLandHectares = request.CultivatedLandHectares,
                    LivestockCount = request.LivestockCount,
                    MachineryImportValue = request.MachineryImportValue,
                    EstimatedCorporateTax = request.EstimatedCorporateTax
                };

                var result = _investmentIncentiveService.CalculateInvestmentIncentives(incentiveRequest);

                return Ok(new
                {
                    BusinessName = request.BusinessName,
                    IsEligible = result.AgribusinessExemption?.IsEligible ?? false,
                    ExemptionDetails = result.AgribusinessExemption,
                    EstimatedAnnualSavings = result.AgribusinessExemption?.EstimatedAnnualSavings ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating agribusiness exemption");
                return BadRequest(new { Error = "Failed to calculate agribusiness exemption" });
            }
        }

        /// <summary>
        /// Calculate renewable energy tax incentives
        /// </summary>
        [HttpPost("renewable-energy-incentives")]
        public IActionResult CalculateRenewableEnergyIncentives([FromBody] RenewableEnergyIncentiveRequest request)
        {
            try
            {
                var incentiveRequest = new InvestmentIncentiveRequest
                {
                    BusinessName = request.BusinessName,
                    BusinessSector = BusinessSector.RenewableEnergy,
                    InvestmentAmount = request.InvestmentAmount,
                    EmployeeCount = request.EmployeeCount,
                    RenewableEnergyEquipmentValue = request.EquipmentValue,
                    EstimatedCorporateTax = request.EstimatedCorporateTax
                };

                var result = _investmentIncentiveService.CalculateInvestmentIncentives(incentiveRequest);

                return Ok(new
                {
                    BusinessName = request.BusinessName,
                    IsEligible = result.RenewableEnergyExemption?.IsEligible ?? false,
                    ExemptionDetails = result.RenewableEnergyExemption,
                    EstimatedAnnualSavings = result.RenewableEnergyExemption?.EstimatedAnnualSavings ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating renewable energy incentives");
                return BadRequest(new { Error = "Failed to calculate renewable energy incentives" });
            }
        }

        /// <summary>
        /// Calculate duty-free import eligibility and savings
        /// </summary>
        [HttpPost("duty-free-import-calculator")]
        public IActionResult CalculateDutyFreeImport([FromBody] DutyFreeImportRequest request)
        {
            try
            {
                var incentiveRequest = new InvestmentIncentiveRequest
                {
                    BusinessName = request.BusinessName,
                    InvestmentAmount = request.InvestmentAmount,
                    IsNewBusiness = request.IsNewBusiness,
                    MachineryImportValue = request.MachineryImportValue
                };

                var result = _investmentIncentiveService.CalculateInvestmentIncentives(incentiveRequest);

                return Ok(new
                {
                    BusinessName = request.BusinessName,
                    IsEligible = result.DutyFreeImportProvisions?.Any() ?? false,
                    DutyFreeProvisions = result.DutyFreeImportProvisions,
                    TotalEstimatedSavings = result.DutyFreeImportProvisions?.Sum(x => x.EstimatedSavings) ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating duty-free import eligibility");
                return BadRequest(new { Error = "Failed to calculate duty-free import eligibility" });
            }
        }

        /// <summary>
        /// Calculate R&D tax deductions (125% deduction rate)
        /// </summary>
        [HttpPost("rd-deduction-calculator")]
        public IActionResult CalculateRAndDDeduction([FromBody] RAndDDeductionRequest request)
        {
            try
            {
                var incentiveRequest = new InvestmentIncentiveRequest
                {
                    BusinessName = request.BusinessName,
                    RAndDExpenses = request.RAndDExpenses
                };

                var result = _investmentIncentiveService.CalculateInvestmentIncentives(incentiveRequest);

                return Ok(new
                {
                    BusinessName = request.BusinessName,
                    IsEligible = result.RAndDDeduction?.IsEligible ?? false,
                    RAndDDeductionDetails = result.RAndDDeduction,
                    EstimatedTaxSavings = result.RAndDDeduction?.EstimatedTaxSavings ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating R&D deduction");
                return BadRequest(new { Error = "Failed to calculate R&D deduction" });
            }
        }

        /// <summary>
        /// Get summary of all Finance Act 2025 incentives and requirements
        /// </summary>
        [HttpGet("incentives-summary")]
        public IActionResult GetIncentivesSummary()
        {
            var summary = new
            {
                FinanceActVersion = "Finance Act 2025",
                EffectiveDate = "January 16, 2025",
                InvestmentIncentives = new
                {
                    EmploymentBasedExemptions = new
                    {
                        FiveYearExemption = new
                        {
                            Requirements = "100+ full-time employees, $5M+ investment, 20%+ local ownership",
                            Benefit = "Full Corporate Income Tax Exemption for 5 years"
                        },
                        TenYearExemption = new
                        {
                            Requirements = "150+ full-time employees, $7.5M+ investment, 20%+ local ownership",
                            Benefit = "Full Corporate Income Tax Exemption for 10 years"
                        }
                    },
                    AgribusinessExemptions = new
                    {
                        Requirements = "Large-scale cultivation or significant livestock investment",
                        Benefits = new[]
                        {
                            "Full Corporate Income Tax Exemption",
                            "Import duty exemptions for farm machinery",
                            "Import duty exemptions for agro-processing equipment",
                            "Import duty exemptions for agro-chemicals"
                        }
                    },
                    RenewableEnergyIncentives = new
                    {
                        Requirements = "Minimum $500K investment, 50+ employees",
                        Benefits = new[]
                        {
                            "Tax exemption on photovoltaic systems",
                            "Tax exemption on energy-efficient appliances",
                            "Import duty exemptions on renewable energy equipment"
                        }
                    },
                    DutyFreeImports = new
                    {
                        NewBusinesses = new
                        {
                            Requirements = "New business with minimum $10M investment",
                            Benefit = "3-year duty-free import of plants and machinery"
                        },
                        ExistingBusinesses = new
                        {
                            Requirements = "Existing business expanding with minimum $5M investment",
                            Benefit = "3-year duty-free import of plants and machinery for expansion"
                        }
                    },
                    RAndDDeductions = new
                    {
                        DeductionRate = "125%",
                        QualifyingExpenses = new[]
                        {
                            "Research and development activities",
                            "Training expenses",
                            "Innovation projects"
                        }
                    }
                },
                Notes = new[]
                {
                    "All incentives are subject to meeting specific criteria and conditions",
                    "Applications must be submitted to relevant government authorities",
                    "Incentives may be subject to periodic review and renewal",
                    "Consult with tax professionals for detailed eligibility assessment"
                }
            };

            return Ok(summary);
        }
    }

    // Request DTOs for specific calculations
    public class EmploymentExemptionCheckRequest
    {
        public string BusinessName { get; set; } = string.Empty;
        public decimal InvestmentAmount { get; set; }
        public int EmployeeCount { get; set; }
        public decimal LocalOwnershipPercentage { get; set; }
        public decimal EstimatedCorporateTax { get; set; }
    }

    public class AgribusinessExemptionRequest
    {
        public string BusinessName { get; set; } = string.Empty;
        public decimal InvestmentAmount { get; set; }
        public decimal CultivatedLandHectares { get; set; }
        public int LivestockCount { get; set; }
        public decimal MachineryImportValue { get; set; }
        public decimal EstimatedCorporateTax { get; set; }
    }

    public class RenewableEnergyIncentiveRequest
    {
        public string BusinessName { get; set; } = string.Empty;
        public decimal InvestmentAmount { get; set; }
        public int EmployeeCount { get; set; }
        public decimal EquipmentValue { get; set; }
        public decimal EstimatedCorporateTax { get; set; }
    }

    public class DutyFreeImportRequest
    {
        public string BusinessName { get; set; } = string.Empty;
        public decimal InvestmentAmount { get; set; }
        public bool IsNewBusiness { get; set; }
        public decimal MachineryImportValue { get; set; }
    }

    public class RAndDDeductionRequest
    {
        public string BusinessName { get; set; } = string.Empty;
        public decimal RAndDExpenses { get; set; }
    }
}