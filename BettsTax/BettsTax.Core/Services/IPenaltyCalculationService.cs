using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    public interface IPenaltyCalculationService
    {
        // Penalty rule management
        Task<Result<List<PenaltyRule>>> GetPenaltyRulesAsync(TaxType taxType, PenaltyType penaltyType);
        Task<Result<PenaltyRule>> GetApplicablePenaltyRuleAsync(TaxType taxType, PenaltyType penaltyType, TaxpayerCategory? category = null);
        Task<Result<bool>> SeedDefaultPenaltyRulesAsync();
        
        // Penalty calculations based on Sierra Leone Finance Act
        Task<Result<PenaltyCalculationResultDto>> CalculateLateFilingPenaltyAsync(
            TaxType taxType, 
            decimal taxLiability, 
            DateTime dueDate, 
            DateTime? filedDate = null,
            TaxpayerCategory? category = null);
            
        Task<Result<PenaltyCalculationResultDto>> CalculateLatePaymentPenaltyAsync(
            TaxType taxType, 
            decimal unpaidAmount, 
            DateTime dueDate, 
            DateTime? paidDate = null,
            TaxpayerCategory? category = null);
            
        Task<Result<PenaltyCalculationResultDto>> CalculateInterestAsync(
            decimal unpaidAmount, 
            DateTime dueDate, 
            DateTime? paidDate = null);
            
        Task<Result<PenaltyCalculationResultDto>> CalculateNonFilingPenaltyAsync(
            TaxType taxType, 
            decimal estimatedLiability, 
            DateTime dueDate,
            TaxpayerCategory? category = null);
            
        Task<Result<PenaltyCalculationResultDto>> CalculateUnderDeclarationPenaltyAsync(
            TaxType taxType, 
            decimal declaredAmount, 
            decimal actualAmount,
            TaxpayerCategory? category = null);
            
        // Compound penalty calculations
        Task<Result<List<PenaltyCalculationResultDto>>> CalculateAllApplicablePenaltiesAsync(
            TaxType taxType,
            decimal taxLiability,
            decimal amountPaid,
            DateTime filingDueDate,
            DateTime paymentDueDate,
            DateTime? filedDate = null,
            DateTime? paidDate = null,
            TaxpayerCategory? category = null);
            
        // Penalty validation and verification
        Task<Result<bool>> ValidatePenaltyCalculationAsync(PenaltyCalculationResultDto calculation);
        Task<Result<string>> GetLegalReferenceAsync(TaxType taxType, PenaltyType penaltyType);

        // Additional methods used by other services
        Task<Result<PenaltyCalculationResultDto>> CalculateLatePenaltyAsync(TaxType taxType, decimal taxLiability, DateTime dueDate, DateTime? actualDate = null);
        Task<Result<PenaltyCalculationResultDto>> CalculatePenaltyAsync(TaxType taxType, decimal amount, DateTime dueDate, DateTime? actualDate = null);
        Task<Result<bool>> RecalculatePenaltiesAsync();
    }
}