using AutoMapper;
using RequestsManagement.Dal.Entities;
using RequestsManagement.Bll.Mapping.Dtos;

namespace RequestsManagement.Bll.Mapping.Profiles;

public sealed class UsersProfile : Profile
{
    public UsersProfile()
    {
        CreateMap<User, UserDto>();
    }
}
