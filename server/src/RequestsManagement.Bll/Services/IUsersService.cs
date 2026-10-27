using RequestsManagement.Bll.Mapping.Dtos;

namespace RequestsManagement.Bll.Services;

public interface IUsersService
{
    /// <summary>Active users, used to pick the assignee of a request.</summary>
    Task<IReadOnlyList<UserDto>> GetActiveUsersAsync(CancellationToken cancellationToken);
}
