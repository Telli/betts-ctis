using BettsTax.Data;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services
{
    public class InvestmentIncentiveCalculationService : IInvestmentIncentiveCalculationService
    {
        private readonly ILogger<InvestmentIncentiveCalculationService> _logger;

        public InvestmentIncentiveCalculationService(ILogger<InvestmentIncentiveCalculationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Calculate investment incentive eligibility and tax benefits based on Finance Act 2025
        /// </summary>
        public InvestmentIncentiveResult CalculateInvestmentIncentives(InvestmentIncentiveRequest request)
        {
            var result = new InvestmentIncentiveResult
            {
                BusinessName = request.BusinessName,
                InvestmentAmount = request.InvestmentAmount,
                EmployeeCount = request.EmployeeCount,
                LocalOwnershipPercentage = request.LocalOwnershipPercentage,
                BusinessSector = request.BusinessSector
            };

            // Check eligibility for various incentive programs
            CheckEmploymentBasedExemptions(request, result);
            CheckAgribusinessExemptions(request, result);
            CheckRenewableEnergyExemptions(request, result);
            CheckDutyFreeImportEligibility(request, result);
            CheckRAndDDeductions(request, result);

            // Calculate total potential tax savings
            CalculateTotalTaxSavings(result, request.AnnualRevenue, request.EstimatedCorporateTax);

            return result;
        }

        /// <summary>
        /// Check eligibility for employment-based corporate tax exemptions (Finance Act 2025)
        /// </summary>
        private void CheckEmploymentBasedExemptions(InvestmentIncentiveRequest request, InvestmentIncentiveResult result)
        {
            // Must be registered in Sierra Leone with at least 20% local ownership
            if (request.LocalOwnershipPercentage < 20)
            {
                result.EmploymentBasedExemption = new EmploymentBasedExemption
                {
                    IsEligible = false,
                    Reason = "Requires at least 20% local ownership"
                };
                return;
            }

            // Check for 5-year exemption (100+ employees, $5M+ investment)
            if (request.EmployeeCount >= 100 && request.InvestmentAmount >= 5_000_000)
            {
                result.EmploymentBasedExemption = new EmploymentBasedExemption
                {
                    IsEligible = true,
                    ExemptionYears = 5,
                    ExemptionType = "Full Corporate Income Tax Exemption",
                    Requirements = "100+ full-time employees, $5M+ investment, 20%+ local ownership",
                    EstimatedAnnualSavings = request.EstimatedCorporateTax // Full exemption
                };
                return;
            }

            // Check for 10-year exemption (150+ employees, $7.5M+ investment)
            if (request.EmployeeCount >= 150 && request.InvestmentAmount >= 7_500_000)
            {
                result.EmploymentBasedExemption = new EmploymentBasedExemption
                {
                    IsEligible = true,
                    ExemptionYears = 10,
                    ExemptionType = "Full Corporate Income Tax Exemption",
                    Requirements = "150+ full-time employees, $7.5M+ investment, 20%+ local ownership",
                    EstimatedAnnualSavings = request.EstimatedCorporateTax // Full exemption
                };
                return;
            }

            result.EmploymentBasedExemption = new EmploymentBasedExemption
            {
                IsEligible = false,
                Reason = "Does not meet minimum employment and investment thresholds"
            };
        }

        /// <summary>
        /// Check eligibility for agribusiness tax exemptions (Finance Act 2025)
        /// </summary>
        private void CheckAgribusinessExemptions(InvestmentIncentiveRequest request, InvestmentIncentiveResult result)
        {
            if (request.BusinessSector != BusinessSector.Agriculture)
            {
                result.AgribusinessExemption = new AgribusinessExemption
                {
                    IsEligible = false,
                    Reason = "Not in agriculture sector"
                };
                return;
            }

            // Check specific agribusiness criteria
            bool meetsLandRequirement = request.CultivatedLandHectares >= 50; // Assume 50+ hectares for "large areas"
            bool meetsLivestockRequirement = request.LivestockCount >= 100; // Assume 100+ livestock
            bool meetsInvestmentRequirement = request.InvestmentAmount >= 100_000; // Minimum investment

            if (meetsLandRequirement || meetsLivestockRequirement)
            {
                var exemptions = new List<string>();
                decimal estimatedSavings = 0;

                // Corporate income tax exemption
                exemptions.Add("Full Corporate Income Tax Exemption");
                estimatedSavings += request.EstimatedCorporateTax;

                // Import duty exemptions for farm machinery and equipment
                if (request.MachineryImportValue > 0)
                {
                    decimal dutySavings = request.MachineryImportValue * 0.20m; // Assume 20% average duty
                    exemptions.Add("Import Duty Exemption on Farm Machinery");
                    estimatedSavings += dutySavings;
                }

                result.AgribusinessExemption = new AgribusinessExemption
                {
                    IsEligible = true,
                    ExemptionType = string.Join(", ", exemptions),
                    Requirements = "Large-scale cultivation or significant livestock investment",
                    EstimatedAnnualSavings = estimatedSavings,
                    QualifyingActivities = new[]
                    {
                        meetsLandRequirement ? $"Cultivating {request.CultivatedLandHectares} hectares" : null,
                        meetsLivestockRequirement ? $"Managing {request.LivestockCount} livestock" : null
                    }.Where(x => x != null).ToArray()
                };
            }
            else
            {
                result.AgribusinessExemption = new AgribusinessExemption
                {
                    IsEligible = false,
                    Reason = "Does not meet large-scale cultivation or livestock investment criteria"
                };
            }
        }

        /// <summary>
        /// Check eligibility for renewable energy incentives (Finance Act 2025)
        /// </summary>
        private void CheckRenewableEnergyExemptions(InvestmentIncentiveRequest request, InvestmentIncentiveResult result)
        {
            if (request.BusinessSector != BusinessSector.RenewableEnergy)
            {
                result.RenewableEnergyExemption = new RenewableEnergyExemption
                {
                    IsEligible = false,
                    Reason = "Not in renewable energy sector"
                };
                return;
            }

            // Minimum requirements: $500K investment, 50+ employees
            if (request.InvestmentAmount >= 500_000 && request.EmployeeCount >= 50)
            {
                var exemptions = new List<string>();
                decimal estimatedSavings = 0;

                // Tax exemptions for qualifying renewable energy investments
                exemptions.Add("Tax Exemption on Photovoltaic Systems");
                exemptions.Add("Tax Exemption on Energy-Efficient Appliances");

                // Calculate potential import duty savings
                if (request.RenewableEnergyEquipmentValue > 0)
                {
                    decimal dutySavings = request.RenewableEnergyEquipmentValue * 0.15m; // Assume 15% average duty
                    estimatedSavings += dutySavings;
                }

                result.RenewableEnergyExemption = new RenewableEnergyExemption
                {
                    IsEligible = true,
                    ExemptionType = string.Join(", ", exemptions),
                    Requirements = "Minimum $500K investment, 50+ employees",
                    EstimatedAnnualSavings = estimatedSavings,
                    QualifyingEquipment = new[]
                    {
                        "Photovoltaic systems",
                        "Energy-efficient appliances",
                        "Other renewable energy products"
                    }
                };
            }
            else
            {
                result.RenewableEnergyExemption = new RenewableEnergyExemption
                {
                    IsEligible = false,
                    Reason = "Does not meet minimum investment ($500K) and employment (50 workers) requirements"
                };
            }
        }

        /// <summary>
        /// Check eligibility for duty-free import provisions (Finance Act 2025)
        /// </summary>
        private void CheckDutyFreeImportEligibility(InvestmentIncentiveRequest request, InvestmentIncentiveResult result)
        {
            var dutyFreeProvisions = new List<DutyFreeProvision>();

            // New businesses with $10M+ investment (3-year duty-free)
            if (request.IsNewBusiness && request.InvestmentAmount >= 10_000_000)
            {
                decimal estimatedSavings = request.MachineryImportValue * 0.20m; // Assume 20% average duty
                dutyFreeProvisions.Add(new DutyFreeProvision
                {
                    Type = "New Business Duty-Free Import",
                    DurationYears = 3,
                    Requirements = "New business with minimum $10M investment",
                    EstimatedSavings = estimatedSavings,
                    QualifyingItems = new[] { "Plants and machinery" }
                });
            }

            // Existing businesses expanding with $5M+ investment
            if (!request.IsNewBusiness && request.InvestmentAmount >= 5_000_000)
            {
                decimal estimatedSavings = request.MachineryImportValue * 0.20m;
                dutyFreeProvisions.Add(new DutyFreeProvision
                {
                    Type = "Business Expansion Duty-Free Import",
                    DurationYears = 3,
                    Requirements = "Existing business expanding with minimum $5M investment",
                    EstimatedSavings = estimatedSavings,
                    QualifyingItems = new[] { "Plants and machinery for expansion" }
                });
            }

            result.DutyFreeImportProvisions = dutyFreeProvisions;
        }

        /// <summary>
        /// Calculate R&D tax deductions (Finance Act 2025 - 125% deduction)
        /// </summary>
        private void CheckRAndDDeductions(InvestmentIncentiveRequest request, InvestmentIncentiveResult result)
        {
            if (request.RAndDExpenses > 0)
            {
                // 125% tax deduction means you can deduct 125% of R&D expenses
                decimal extraDeduction = request.RAndDExpenses * 0.25m; // Extra 25% on top of normal 100%
                decimal taxSavings = extraDeduction * 0.25m; // Apply corporate tax rate to extra deduction

                result.RAndDDeduction = new RAndDDeduction
                {
                    IsEligible = true,
                    DeductionRate = 125,
                    RAndDExpenses = request.RAndDExpenses,
                    ExtraDeductionAmount = extraDeduction,
                    EstimatedTaxSavings = taxSavings,
                    QualifyingExpenses = new[]
                    {
                        "Research and development activities",
                        "Training expenses",
                        "Innovation projects"
                    }
                };
            }
            else
            {
                result.RAndDDeduction = new RAndDDeduction
                {
                    IsEligible = false,
                    Reason = "No R&D expenses declared"
                };
            }
        }

        /// <summary>
        /// Calculate total potential tax savings from all incentives
        /// </summary>
        private void CalculateTotalTaxSavings(InvestmentIncentiveResult result, decimal annualRevenue, decimal estimatedCorporateTax)
        {
            decimal totalSavings = 0;

            // Employment-based exemptions
            if (result.EmploymentBasedExemption?.IsEligible == true)
            {
                totalSavings += result.EmploymentBasedExemption.EstimatedAnnualSavings;
            }

            // Agribusiness exemptions
            if (result.AgribusinessExemption?.IsEligible == true)
            {
                totalSavings += result.AgribusinessExemption.EstimatedAnnualSavings;
            }

            // Renewable energy exemptions
            if (result.RenewableEnergyExemption?.IsEligible == true)
            {
                totalSavings += result.RenewableEnergyExemption.EstimatedAnnualSavings;
            }

            // Duty-free import savings
            if (result.DutyFreeImportProvisions?.Any() == true)
            {
                totalSavings += result.DutyFreeImportProvisions.Sum(x => x.EstimatedSavings);
            }

            // R&D deduction savings
            if (result.RAndDDeduction?.IsEligible == true)
            {
                totalSavings += result.RAndDDeduction.EstimatedTaxSavings;
            }

            result.TotalEstimatedAnnualSavings = totalSavings;
            result.SavingsAsPercentageOfRevenue = annualRevenue > 0 ? (totalSavings / annualRevenue) * 100 : 0;
        }
    }

    // Supporting enums and classes
    public enum BusinessSector
    {
        Agriculture,
        Manufacturing,
        Services,
        RenewableEnergy,
        Mining,
        Tourism,
        Technology,
        Other
    }

    public class InvestmentIncentiveRequest
    {
        public string BusinessName { get; set; } = string.Empty;
        public decimal InvestmentAmount { get; set; }
        public int EmployeeCount { get; set; }
        public decimal LocalOwnershipPercentage { get; set; }
        public BusinessSector BusinessSector { get; set; }
        public bool IsNewBusiness { get; set; }
        public decimal AnnualRevenue { get; set; }
        public decimal EstimatedCorporateTax { get; set; }
        
        // Agriculture specific
        public decimal CultivatedLandHectares { get; set; }
        public int LivestockCount { get; set; }
        public decimal MachineryImportValue { get; set; }
        
        // Renewable energy specific
        public decimal RenewableEnergyEquipmentValue { get; set; }
        
        // R&D specific
        public decimal RAndDExpenses { get; set; }
    }

    public class InvestmentIncentiveResult
    {
        public string BusinessName { get; set; } = string.Empty;
        public decimal InvestmentAmount { get; set; }
        public int EmployeeCount { get; set; }
        public decimal LocalOwnershipPercentage { get; set; }
        public BusinessSector BusinessSector { get; set; }
        
        public EmploymentBasedExemption? EmploymentBasedExemption { get; set; }
        public AgribusinessExemption? AgribusinessExemption { get; set; }
        public RenewableEnergyExemption? RenewableEnergyExemption { get; set; }
        public List<DutyFreeProvision> DutyFreeImportProvisions { get; set; } = new();
        public RAndDDeduction? RAndDDeduction { get; set; }
        
        public decimal TotalEstimatedAnnualSavings { get; set; }
        public decimal SavingsAsPercentageOfRevenue { get; set; }
        
        public DateTime CalculationDate { get; set; } = DateTime.UtcNow;
        public string FinanceActVersion { get; set; } = "Finance Act 2025";
    }

    public class EmploymentBasedExemption
    {
        public bool IsEligible { get; set; }
        public int ExemptionYears { get; set; }
        public string ExemptionType { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public decimal EstimatedAnnualSavings { get; set; }
        public string? Reason { get; set; }
    }

    public class AgribusinessExemption
    {
        public bool IsEligible { get; set; }
        public string ExemptionType { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public decimal EstimatedAnnualSavings { get; set; }
        public string[]? QualifyingActivities { get; set; }
        public string? Reason { get; set; }
    }

    public class RenewableEnergyExemption
    {
        public bool IsEligible { get; set; }
        public string ExemptionType { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public decimal EstimatedAnnualSavings { get; set; }
        public string[]? QualifyingEquipment { get; set; }
        public string? Reason { get; set; }
    }

    public class DutyFreeProvision
    {
        public string Type { get; set; } = string.Empty;
        public int DurationYears { get; set; }
        public string Requirements { get; set; } = string.Empty;
        public decimal EstimatedSavings { get; set; }
        public string[] QualifyingItems { get; set; } = Array.Empty<string>();
    }

    public class RAndDDeduction
    {
        public bool IsEligible { get; set; }
        public decimal DeductionRate { get; set; } // 125%
        public decimal RAndDExpenses { get; set; }
        public decimal ExtraDeductionAmount { get; set; }
        public decimal EstimatedTaxSavings { get; set; }
        public string[]? QualifyingExpenses { get; set; }
        public string? Reason { get; set; }
    }
}