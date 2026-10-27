using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using RequestsManagement.Dal;
using RequestsManagement.Dal.Entities;

namespace RequestsManagement.Tests.Integration;

// Runs the real API in memory against a dedicated SQL Server database, so RowVersion and
// optimistic concurrency are enforced by the real engine. The database is created and dropped by the tests,
// and never touches the development database.
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DefaultConnectionString =
        "Server=localhost;Database=RequestsManagement_Tests;Trusted_Connection=True;TrustServerCertificate=True;";

    // Override with TESTS_CONNECTION_STRING to run against another SQL Server instance.
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TESTS_CONNECTION_STRING") ?? DefaultConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
    }

    // Empties the tables and the summary cache so every test starts from a known state.
    // Returns the id of an active user that tests can use as ChangedByUserId.
    public async Task<int> ResetAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.RequestStatusHistory.ExecuteDeleteAsync();
        await db.Requests.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();

        var user = new User { UserName = "tester", DisplayName = "Test User", CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        (scope.ServiceProvider.GetRequiredService<IMemoryCache>() as MemoryCache)?.Compact(1.0);
        return user.Id;
    }

    public async Task<Request> AddRequestAsync(
        string title = "Test request",
        string organization = "Test Org",
        RequestStatus status = RequestStatus.New,
        RequestPriority priority = RequestPriority.Medium,
        DateTime? createdAt = null)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var created = createdAt ?? DateTime.UtcNow;
        var request = new Request
        {
            Title = title,
            OrganizationName = organization,
            Status = status,
            Priority = priority,
            CreatedAt = created,
            UpdatedAt = created,
        };
        db.Requests.Add(request);
        await db.SaveChangesAsync();
        return request;
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
