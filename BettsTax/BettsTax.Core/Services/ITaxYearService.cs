using BettsTax.Core.DTOs;

namespace BettsTax.Core.Services
{
    public interface ITaxYearService
    {
        Task<IEnumerable<TaxYearDto>> GetClientTaxYearsAsync(int clientId);
        Task<TaxYearDto> CreateAsync(CreateTaxYearDto dto);
    }
}
