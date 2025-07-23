using AutoMapper;
using BettsTax.Core.DTOs;
using BettsTax.Data;

namespace BettsTax.Core.Profiles
{
    public class ClientProfile : Profile
    {
        public ClientProfile()
        {
            CreateMap<Client, ClientDto>().ReverseMap();
        }
    }
}
