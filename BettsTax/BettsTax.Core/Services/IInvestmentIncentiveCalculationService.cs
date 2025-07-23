namespace BettsTax.Core.Services
{
    public interface IInvestmentIncentiveCalculationService
    {
        /// <summary>
        /// Calculate investment incentive eligibility and tax benefits based on Finance Act 2025
        /// </summary>
        InvestmentIncentiveResult CalculateInvestmentIncentives(InvestmentIncentiveRequest request);
    }
}