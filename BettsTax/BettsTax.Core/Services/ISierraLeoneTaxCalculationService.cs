using BettsTax.Data;

namespace BettsTax.Core.Services
{
    public interface ISierraLeoneTaxCalculationService
    {
        /// <summary>
        /// Calculate Income Tax based on Sierra Leone Finance Act 2024
        /// </summary>
        decimal CalculateIncomeTax(decimal taxableIncome, TaxpayerCategory category, bool isIndividual = false);

        /// <summary>
        /// Calculate GST (Goods and Services Tax) - 15% standard rate
        /// </summary>
        decimal CalculateGST(decimal taxableAmount, string itemCategory = "standard");

        /// <summary>
        /// Calculate Withholding Tax based on Finance Act 2024
        /// </summary>
        decimal CalculateWithholdingTax(decimal amount, WithholdingTaxType type, bool isResident = true);

        /// <summary>
        /// Calculate PAYE (Pay As You Earn) tax for employees
        /// </summary>
        decimal CalculatePAYE(decimal grossSalary, decimal allowances = 0);

        /// <summary>
        /// Calculate penalties for late filing or payment
        /// </summary>
        decimal CalculatePenalty(decimal taxAmount, int daysLate, PenaltyType penaltyType);

        /// <summary>
        /// Calculate interest on late payments
        /// </summary>
        decimal CalculateInterest(decimal principalAmount, int daysLate, decimal annualInterestRate = 0.15m);

        /// <summary>
        /// Calculate minimum tax for companies
        /// </summary>
        decimal CalculateMinimumTax(decimal annualTurnover);

        /// <summary>
        /// Calculate total tax liability including penalties and interest
        /// </summary>
        TaxCalculationResult CalculateTotalTaxLiability(
            decimal taxableAmount,
            TaxType taxType,
            TaxpayerCategory category,
            DateTime dueDate,
            decimal annualTurnover = 0,
            bool isIndividual = false);
    }
}