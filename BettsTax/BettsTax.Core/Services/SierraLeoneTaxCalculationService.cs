using BettsTax.Data;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services
{
    public class SierraLeoneTaxCalculationService : ISierraLeoneTaxCalculationService
    {
        private readonly ILogger<SierraLeoneTaxCalculationService> _logger;

        public SierraLeoneTaxCalculationService(ILogger<SierraLeoneTaxCalculationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Calculate Income Tax based on Sierra Leone Finance Act 2024
        /// </summary>
        public decimal CalculateIncomeTax(decimal taxableIncome, TaxpayerCategory category, bool isIndividual = false)
        {
            if (isIndividual)
            {
                return CalculateIndividualIncomeTax(taxableIncome);
            }

            // Corporate Income Tax - flat 25% for all companies (Finance Act 2024)
            return taxableIncome * 0.25m;
        }

        /// <summary>
        /// Calculate Individual Income Tax using progressive tax brackets (Sierra Leone)
        /// Amounts in Sierra Leone Leones (SLE)
        /// </summary>
        private decimal CalculateIndividualIncomeTax(decimal taxableIncome)
        {
            decimal tax = 0;
            
            // Tax brackets for 2024 (amounts in SLE thousands)
            var brackets = new[]
            {
                new { Threshold = 600_000m, Rate = 0.00m },      // First 600k SLE - 0%
                new { Threshold = 1_200_000m, Rate = 0.15m },    // Next 600k SLE - 15%
                new { Threshold = 1_800_000m, Rate = 0.20m },    // Next 600k SLE - 20%
                new { Threshold = 2_400_000m, Rate = 0.25m },    // Next 600k SLE - 25%
                new { Threshold = decimal.MaxValue, Rate = 0.30m } // Above 2.4M SLE - 30%
            };

            decimal remainingIncome = taxableIncome;
            decimal previousThreshold = 0;

            foreach (var bracket in brackets)
            {
                if (remainingIncome <= 0) break;

                decimal bracketAmount = Math.Min(remainingIncome, bracket.Threshold - previousThreshold);
                tax += bracketAmount * bracket.Rate;

                remainingIncome -= bracketAmount;
                previousThreshold = bracket.Threshold;

                if (bracket.Threshold == decimal.MaxValue) break;
            }

            return Math.Round(tax, 2);
        }

        /// <summary>
        /// Calculate GST (Goods and Services Tax) - 15% standard rate
        /// </summary>
        public decimal CalculateGST(decimal taxableAmount, string itemCategory = "standard")
        {
            // Sierra Leone GST is 15% for most goods and services
            var gstRate = itemCategory.ToLower() switch
            {
                "exempt" => 0.00m,           // Exempt items (basic food, medical, etc.)
                "zero-rated" => 0.00m,      // Zero-rated exports
                "standard" => 0.15m,        // Standard rate - 15%
                _ => 0.15m                  // Default to standard rate
            };

            return Math.Round(taxableAmount * gstRate, 2);
        }

        /// <summary>
        /// Calculate Withholding Tax based on Finance Act 2024 (increased from 10% to 15%)
        /// </summary>
        public decimal CalculateWithholdingTax(decimal amount, WithholdingTaxType type, bool isResident = true)
        {
            decimal rate = type switch
            {
                WithholdingTaxType.Dividends => 0.15m,                    // 15% (increased from 10% in 2024)
                WithholdingTaxType.ManagementFees => 0.15m,               // 15% (increased from 10% in 2024)
                WithholdingTaxType.ProfessionalFees => 0.15m,             // 15% (increased from 10% in 2024)
                WithholdingTaxType.LotteryWinnings => isResident ? 0.15m : 0.15m, // 15% for residents
                WithholdingTaxType.Royalties => 0.15m,                    // 15%
                WithholdingTaxType.Interest => 0.15m,                     // 15%
                WithholdingTaxType.Rent => 0.10m,                        // 10%
                WithholdingTaxType.Commissions => 0.05m,                 // 5%
                _ => 0.15m                                                // Default 15%
            };

            return Math.Round(amount * rate, 2);
        }

        /// <summary>
        /// Calculate PAYE (Pay As You Earn) tax for employees
        /// </summary>
        public decimal CalculatePAYE(decimal grossSalary, decimal allowances = 0)
        {
            // PAYE uses the same progressive rates as individual income tax
            decimal taxableIncome = grossSalary + allowances;
            return CalculateIndividualIncomeTax(taxableIncome);
        }

        /// <summary>
        /// Calculate penalties for late filing or payment
        /// </summary>
        public decimal CalculatePenalty(decimal taxAmount, int daysLate, PenaltyType penaltyType)
        {
            decimal penalty = 0;

            switch (penaltyType)
            {
                case PenaltyType.LateFilingPenalty:
                    // Fixed penalty for late filing (typically 5% of tax due or minimum amount)
                    penalty = Math.Max(taxAmount * 0.05m, 50_000m); // 5% or minimum 50k SLE
                    break;

                case PenaltyType.LatePaymentPenalty:
                    // Progressive penalty for late payment
                    if (daysLate <= 30)
                        penalty = taxAmount * 0.05m; // 5% for first 30 days
                    else if (daysLate <= 60)
                        penalty = taxAmount * 0.10m; // 10% for 31-60 days
                    else
                        penalty = taxAmount * 0.15m; // 15% for over 60 days
                    break;

                case PenaltyType.UnderDeclarationPenalty:
                    // Penalty for under-declaring income (typically 20% of additional tax)
                    penalty = taxAmount * 0.20m;
                    break;
            }

            return Math.Round(penalty, 2);
        }

        /// <summary>
        /// Calculate interest on late payments
        /// </summary>
        public decimal CalculateInterest(decimal principalAmount, int daysLate, decimal annualInterestRate = 0.15m)
        {
            if (daysLate <= 0) return 0;

            // Calculate daily interest rate
            decimal dailyRate = annualInterestRate / 365m;
            decimal interest = principalAmount * dailyRate * daysLate;

            return Math.Round(interest, 2);
        }

        /// <summary>
        /// Calculate minimum tax for companies (typically 0.5% of turnover)
        /// </summary>
        public decimal CalculateMinimumTax(decimal annualTurnover)
        {
            // Minimum tax is typically 0.5% of annual turnover
            decimal minimumTaxRate = 0.005m; // 0.5%
            return Math.Round(annualTurnover * minimumTaxRate, 2);
        }

        /// <summary>
        /// Calculate Minimum Alternate Tax (MAT) introduced in Finance Act 2023
        /// MAT is 3% on turnover, payable if greater than calculated CIT
        /// </summary>
        public decimal CalculateMinimumAlternateTax(decimal annualTurnover)
        {
            // Finance Act 2023: MAT at 3% on turnover
            decimal matRate = 0.03m; // 3%
            return Math.Round(annualTurnover * matRate, 2);
        }

        /// <summary>
        /// Determine if minimum tax applies (when calculated income tax is less than minimum tax)
        /// </summary>
        public decimal GetApplicableTax(decimal calculatedIncomeTax, decimal minimumTax)
        {
            return Math.Max(calculatedIncomeTax, minimumTax);
        }

        /// <summary>
        /// Determine applicable tax considering both minimum tax and MAT (Finance Act 2023)
        /// </summary>
        public decimal GetApplicableTaxWithMAT(decimal calculatedIncomeTax, decimal minimumTax, decimal minimumAlternateTax)
        {
            // The highest of calculated CIT, minimum tax, or MAT applies
            return Math.Max(Math.Max(calculatedIncomeTax, minimumTax), minimumAlternateTax);
        }

        /// <summary>
        /// Calculate total tax liability including penalties and interest
        /// </summary>
        public TaxCalculationResult CalculateTotalTaxLiability(
            decimal taxableAmount,
            TaxType taxType,
            TaxpayerCategory category,
            DateTime dueDate,
            decimal annualTurnover = 0,
            bool isIndividual = false)
        {
            var result = new TaxCalculationResult();
            
            // Calculate base tax
            result.BaseTax = taxType switch
            {
                TaxType.IncomeTax => CalculateIncomeTax(taxableAmount, category, isIndividual),
                TaxType.GST => CalculateGST(taxableAmount),
                TaxType.PayrollTax => CalculatePAYE(taxableAmount),
                _ => 0
            };

            // Calculate minimum tax and MAT for corporate income tax
            if (taxType == TaxType.IncomeTax && !isIndividual && annualTurnover > 0)
            {
                decimal minimumTax = CalculateMinimumTax(annualTurnover);
                decimal minimumAlternateTax = CalculateMinimumAlternateTax(annualTurnover);
                
                result.MinimumTax = minimumTax;
                result.MinimumAlternateTax = minimumAlternateTax;
                result.BaseTax = GetApplicableTaxWithMAT(result.BaseTax, minimumTax, minimumAlternateTax);
            }

            // Calculate penalties and interest if payment is late
            if (DateTime.UtcNow > dueDate)
            {
                int daysLate = (DateTime.UtcNow - dueDate).Days;
                result.Penalty = CalculatePenalty(result.BaseTax, daysLate, PenaltyType.LatePaymentPenalty);
                result.Interest = CalculateInterest(result.BaseTax, daysLate);
            }

            result.TotalTaxLiability = result.BaseTax + result.Penalty + result.Interest;
            result.CalculationDate = DateTime.UtcNow;

            return result;
        }
    }

    public enum WithholdingTaxType
    {
        Dividends,
        ManagementFees,
        ProfessionalFees,
        LotteryWinnings,
        Royalties,
        Interest,
        Rent,
        Commissions
    }


    public class TaxCalculationResult
    {
        public decimal BaseTax { get; set; }
        public decimal MinimumTax { get; set; }
        public decimal MinimumAlternateTax { get; set; } // Finance Act 2023
        public decimal Penalty { get; set; }
        public decimal Interest { get; set; }
        public decimal TotalTaxLiability { get; set; }
        public DateTime CalculationDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string ApplicableTaxType { get; set; } = string.Empty; // Which tax rate was applied
    }
}