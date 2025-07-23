using AutoMapper;
using BettsTax.Core.DTOs;
using BettsTax.Data;

namespace BettsTax.Core.Profiles
{
    public class PaymentProfile : Profile
    {
        public PaymentProfile()
        {
            CreateMap<Payment, PaymentDto>().ReverseMap();
            CreateMap<CreatePaymentDto, Payment>();
        }
    }
}
