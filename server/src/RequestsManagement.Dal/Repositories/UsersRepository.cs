using Microsoft.EntityFrameworkCore;
using RequestsManagement.Dal.Entities;

namespace RequestsManagement.Dal.Repositories;

internal sealed class UsersRepository(AppDbContext db) : IUsersRepository
{
    public async Task<List<User>> GetActiveAsync(CancellationToken cancellationToken)
    {
        return await db.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);
    }
}
