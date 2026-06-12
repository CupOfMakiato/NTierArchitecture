using AutoMapper;
using NTierArchitecture.Application.DTOs.User;
using NTierArchitecture.Domain.Entities;

namespace NTierArchitecture.Application.Mappers
{
    public class MapperConfigurationsProfile : Profile
    {
        public MapperConfigurationsProfile()
        {
            CreateMap<User, UserDTO>();
        }
    }
}
