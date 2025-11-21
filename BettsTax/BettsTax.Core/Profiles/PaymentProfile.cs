using AutoMapper;
using BettsTax.Core.DTOs;
using BettsTax.Data;

namespace BettsTax.Core.Profiles
{
    public class PaymentProfile : Profile
    {
        public PaymentProfile()
        {
            CreateMap<Payment, PaymentDto>()
                .ForMember(dto => dto.ClientName,
                    opt => opt.MapFrom(src => src.Client != null ? src.Client.BusinessName ?? string.Empty : string.Empty))
                .ForMember(dto => dto.ClientNumber,
                    opt => opt.MapFrom(src => src.Client != null ? src.Client.ClientNumber ?? string.Empty : string.Empty))
                .ReverseMap()
                .ForMember(entity => entity.Client, opt => opt.Ignore());
            CreateMap<CreatePaymentDto, Payment>();
            CreateMap<PaymentTransaction, PaymentTransactionDto>()
                .ForMember(dto => dto.ProviderName, opt => opt.Ignore())
                .ForMember(dto => dto.StatusDescription, opt => opt.Ignore())
                .ForMember(dto => dto.ClientName, opt => opt.Ignore())
                .ForMember(dto => dto.PaymentReference, opt => opt.Ignore());
            CreateMap<PaymentMethodConfig, PaymentMethodConfigDto>()
                .ForMember(dto => dto.EstimatedFee, opt => opt.Ignore())
                .ForMember(dto => dto.FeeDescription, opt => opt.Ignore());
            CreateMap<PaymentProviderConfig, PaymentProviderConfigDto>();
        }
    }
}
