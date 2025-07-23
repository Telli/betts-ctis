using AutoMapper;
using BettsTax.Core.DTOs;
using BettsTax.Data;
using Microsoft.EntityFrameworkCore;

namespace BettsTax.Core.Services
{
    public class TaxYearService : ITaxYearService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public TaxYearService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TaxYearDto>> GetClientTaxYearsAsync(int clientId)
        {
            var list = await _db.TaxYears.Where(t => t.ClientId == clientId).ToListAsync();
            return _mapper.Map<IEnumerable<TaxYearDto>>(list);
        }

        public async Task<TaxYearDto> CreateAsync(CreateTaxYearDto dto)
        {
            var entity = _mapper.Map<TaxYear>(dto);
            _db.TaxYears.Add(entity);
            await _db.SaveChangesAsync();
            return _mapper.Map<TaxYearDto>(entity);
        }
    }
}
