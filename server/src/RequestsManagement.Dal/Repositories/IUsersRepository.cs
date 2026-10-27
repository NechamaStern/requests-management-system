using RequestsManagement.Dal.Entities;

namespace RequestsManagement.Dal.Repositories;

public interface IUsersRepository
{
    /// <summary>Active users ordered by display name.</summary>
    Task<List<User>> GetActiveAsync(CancellationToken cancellationToken);
}
