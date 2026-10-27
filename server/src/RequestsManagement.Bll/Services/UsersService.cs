using AutoMapper;
using RequestsManagement.Bll.Mapping.Dtos;
using RequestsManagement.Dal.Repositories;

namespace RequestsManagement.Bll.Services;

internal sealed class UsersService(IUsersRepository usersRepository, IMapper mapper) : IUsersService
{
    public async Task<IReadOnlyList<UserDto>> GetActiveUsersAsync(CancellationToken cancellationToken)
    {
        return mapper.Map<List<UserDto>>(await usersRepository.GetActiveAsync(cancellationToken));
    }
}
