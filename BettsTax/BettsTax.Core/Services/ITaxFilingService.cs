using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    public interface ITaxFilingService
    {
        Task<PagedResult<TaxFilingDto>> GetTaxFilingsAsync(int page, int pageSize, string? searchTerm = null, TaxType? taxType = null, FilingStatus? status = null, int? clientId = null);
        Task<TaxFilingDto?> GetTaxFilingByIdAsync(int id);
        Task<List<TaxFilingDto>> GetTaxFilingsByClientIdAsync(int clientId);
        Task<TaxFilingDto> CreateTaxFilingAsync(CreateTaxFilingDto createDto, string userId);
        Task<TaxFilingDto> UpdateTaxFilingAsync(int id, UpdateTaxFilingDto updateDto, string userId);
        Task<bool> DeleteTaxFilingAsync(int id, string userId);
        Task<TaxFilingDto> SubmitTaxFilingAsync(int id, string userId);
        Task<TaxFilingDto> ReviewTaxFilingAsync(int id, ReviewTaxFilingDto reviewDto, string userId);
        Task<List<TaxFilingDto>> GetUpcomingDeadlinesAsync(int days = 30);
	        Task<TaxFilingValidationResultDto> ValidateTaxFilingForSubmissionAsync(int id);

        Task<decimal> CalculateTaxLiabilityAsync(int clientId, TaxType taxType, int taxYear, decimal taxableAmount, decimal annualTurnover = 0, bool isIndividual = false);
        Task<TaxCalculationResult> CalculateComprehensiveTaxLiabilityAsync(int clientId, TaxType taxType, int taxYear, decimal taxableAmount, DateTime dueDate, decimal annualTurnover = 0, bool isIndividual = false);

        // Associate on-behalf methods
        Task<List<TaxFilingDto>> GetTaxFilingsForClientsAsync(List<int> clientIds, string? searchTerm = null, TaxType? taxType = null, FilingStatus? status = null);
        Task<TaxFilingDto> CreateTaxFilingOnBehalfAsync(CreateTaxFilingDto createDto, string associateId, int clientId);
        Task<TaxFilingDto> UpdateTaxFilingOnBehalfAsync(int id, UpdateTaxFilingDto updateDto, string associateId, int clientId);
        Task<TaxFilingDto> SubmitTaxFilingOnBehalfAsync(int id, string associateId, int clientId);
    }
}