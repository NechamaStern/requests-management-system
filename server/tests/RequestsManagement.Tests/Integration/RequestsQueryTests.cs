using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RequestsManagement.Dal.Entities;

namespace RequestsManagement.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class RequestsQueryTests(ApiFactory factory)
{
    [Fact]
    public async Task Search_CombinesFiltersSortsAndPagesOnTheServer()
    {
        await factory.ResetAsync();
        var now = DateTime.UtcNow;
        await factory.AddRequestAsync("Alpha", "Acme", RequestStatus.New, RequestPriority.High, now);
        await factory.AddRequestAsync("Bravo", "Acme", RequestStatus.New, RequestPriority.High, now);
        await factory.AddRequestAsync("Charlie", "Acme", RequestStatus.New, RequestPriority.High, now);
        await factory.AddRequestAsync("Delta", "Acme", RequestStatus.Completed, RequestPriority.High, now);
        await factory.AddRequestAsync("Echo", "Other", RequestStatus.New, RequestPriority.High, now);
        await factory.AddRequestAsync("Foxtrot", "Acme", RequestStatus.New, RequestPriority.Low, now);

        var body = await GetAsync("/api/requests?status=New&priority=High&organizationName=Acme" +
                                  "&sortBy=Title&sortDirection=Desc&page=2&pageSize=2");

        // Matching: Alpha, Bravo, Charlie. Sorted by title desc: Charlie, Bravo | Alpha.
        Assert.Equal(3, body.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, body.GetProperty("totalPages").GetInt32());
        var items = body.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("Alpha", items[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task Search_TextMatchesTitleAndOrganizationName()
    {
        await factory.ResetAsync();
        await factory.AddRequestAsync("Broken printer", "Acme");
        await factory.AddRequestAsync("Unrelated", "Printer Works Ltd");
        await factory.AddRequestAsync("Something else", "Other");

        var body = await GetAsync("/api/requests?search=printer");

        Assert.Equal(2, body.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Search_FiltersByCreatedDateRange()
    {
        await factory.ResetAsync();
        await factory.AddRequestAsync("Old", createdAt: new DateTime(2024, 1, 10));
        await factory.AddRequestAsync("InRange", createdAt: new DateTime(2024, 6, 15));
        await factory.AddRequestAsync("TooNew", createdAt: new DateTime(2025, 1, 1));

        var body = await GetAsync("/api/requests?createdFrom=2024-06-01&createdTo=2024-06-30");

        Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
        Assert.Equal("InRange", body.GetProperty("items")[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task Summary_CountsByStatusAndPriority()
    {
        await factory.ResetAsync();
        await factory.AddRequestAsync(status: RequestStatus.New, priority: RequestPriority.High);
        await factory.AddRequestAsync(status: RequestStatus.New, priority: RequestPriority.Low);
        await factory.AddRequestAsync(status: RequestStatus.Completed, priority: RequestPriority.Low);

        var body = await GetAsync("/api/requests/summary");

        Assert.Equal(3, body.GetProperty("total").GetInt32());
        Assert.Equal(2, CountFor(body.GetProperty("byStatus"), "New"));
        Assert.Equal(2, CountFor(body.GetProperty("byPriority"), "Low"));
    }

    private async Task<JsonElement> GetAsync(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static int CountFor(JsonElement counts, string key) =>
        counts.EnumerateArray()
            .Where(c => c.GetProperty("key").GetString() == key)
            .Select(c => c.GetProperty("count").GetInt32())
            .SingleOrDefault();
}
