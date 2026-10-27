using Microsoft.Extensions.DependencyInjection;
using RequestsManagement.Bll.Services;
using RequestsManagement.Dal;

namespace RequestsManagement.Bll;

public static class BllServiceCollectionExtensions
{
    // The API only knows the BL; the BL registers its own dependency on the DAL.
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services, string connectionString)
    {
        services.AddDataLayer(connectionString);
        services.AddMemoryCache();
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(BllServiceCollectionExtensions).Assembly));
        services.AddScoped<IRequestsService, RequestsService>();
        services.AddScoped<IUsersService, UsersService>();
        return services;
    }
}
