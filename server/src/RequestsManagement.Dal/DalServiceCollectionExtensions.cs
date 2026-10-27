using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequestsManagement.Dal.Repositories;

namespace RequestsManagement.Dal;

public static class DalServiceCollectionExtensions
{
    public static IServiceCollection AddDataLayer(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IRequestsRepository, RequestsRepository>();
        services.AddScoped<IUsersRepository, UsersRepository>();
        return services;
    }
}
