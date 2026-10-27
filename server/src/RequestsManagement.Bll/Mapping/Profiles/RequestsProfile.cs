using AutoMapper;
using RequestsManagement.Bll.Mapping.Dtos;
using RequestsManagement.Dal.Models;

namespace RequestsManagement.Bll.Mapping.Profiles;

public sealed class RequestsProfile : Profile
{
    public RequestsProfile()
    {
        CreateMap<RequestsQueryDto, RequestSearchCriteria>();
        CreateMap<RequestListItem, RequestListItemDto>();
        CreateMap<StatusHistoryItem, StatusHistoryItemDto>();
    }
}
