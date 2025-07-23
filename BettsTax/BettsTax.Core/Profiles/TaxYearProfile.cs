using AutoMapper;
using BettsTax.Core.DTOs;
using BettsTax.Data;

namespace BettsTax.Core.Profiles
{
    public class TaxYearProfile : Profile
    {
        public TaxYearProfile()
        {
            CreateMap<TaxYear, TaxYearDto>().ReverseMap();
            CreateMap<CreateTaxYearDto, TaxYear>();
        }
    }
}
