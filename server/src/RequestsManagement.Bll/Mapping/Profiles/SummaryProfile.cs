using AutoMapper;
using RequestsManagement.Bll.Mapping.Dtos;
using RequestsManagement.Dal.Models;

namespace RequestsManagement.Bll.Mapping.Profiles;

public sealed class SummaryProfile : Profile
{
    public SummaryProfile()
    {
        CreateMap(typeof(GroupCount<>), typeof(CountItemDto));
    }
}
