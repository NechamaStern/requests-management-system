using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RequestsManagement.Dal.Entities;

namespace RequestsManagement.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class ConcurrencyTests(ApiFactory factory)
{
    [Fact]
    public async Task Update_TwoParallelRequestsWithSameRowVersion_OnlyOneSucceedsAndTheOtherGets409()
    {
        var userId = await factory.ResetAsync();
        var seeded = await factory.AddRequestAsync(status: RequestStatus.New);
        var client = factory.CreateClient();
        var current = await StatusUpdateTests.GetRequestAsync(client, seeded.Id);

        // Both callers read the same version and send the update at the same time.
        var first = client.PutAsJsonAsync($"/api/requests/{seeded.Id}",
            StatusUpdateTests.UpdateBody("InProgress", current, userId));
        var second = client.PutAsJsonAsync($"/api/requests/{seeded.Id}",
            StatusUpdateTests.UpdateBody("InProgress", current, userId));
        var responses = await Task.WhenAll(first, second);

        var codes = responses.Select(r => r.StatusCode).OrderBy(c => (int)c).ToArray();
        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.Conflict], codes);

        // No lost update, and exactly one audit row.
        var history = await client.GetFromJsonAsync<JsonElement>($"/api/requests/{seeded.Id}/history");
        Assert.Equal(1, history.GetArrayLength());
    }

    [Fact]
    public async Task Update_WithStaleRowVersion_Returns409AndKeepsTheFirstChange()
    {
        var userId = await factory.ResetAsync();
        var seeded = await factory.AddRequestAsync(status: RequestStatus.New);
        var client = factory.CreateClient();
        var stale = await StatusUpdateTests.GetRequestAsync(client, seeded.Id);

        var ok = await client.PutAsJsonAsync($"/api/requests/{seeded.Id}",
            StatusUpdateTests.UpdateBody("InProgress", stale, userId));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        // Valid transition from the now-current state (InProgress -> Waiting), but with the old RowVersion.
        var conflict = await client.PutAsJsonAsync($"/api/requests/{seeded.Id}",
            StatusUpdateTests.UpdateBody("Waiting", stale, userId));

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        var after = await StatusUpdateTests.GetRequestAsync(client, seeded.Id);
        Assert.Equal("InProgress", after.GetProperty("status").GetString());
    }

    [Fact]
    public async Task BulkUpdate_MixedItems_ReturnsPartialSuccessWithPerItemReasons()
    {
        var userId = await factory.ResetAsync();
        var good = await factory.AddRequestAsync("Good", status: RequestStatus.New);
        var stale = await factory.AddRequestAsync("Stale", status: RequestStatus.New);
        var client = factory.CreateClient();
        var goodCurrent = await StatusUpdateTests.GetRequestAsync(client, good.Id);
        var staleCurrent = await StatusUpdateTests.GetRequestAsync(client, stale.Id);

        // Make the second item's RowVersion stale by changing it behind the caller's back.
        var change = await client.PutAsJsonAsync($"/api/requests/{stale.Id}",
            StatusUpdateTests.UpdateBody("InProgress", staleCurrent, userId));
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);

        var response = await client.PostAsJsonAsync("/api/requests/bulk-status", new
        {
            status = "InProgress",
            changedByUserId = userId,
            items = new[]
            {
                new { id = good.Id, rowVersion = goodCurrent.GetProperty("rowVersion").GetString() },
                new { id = stale.Id, rowVersion = staleCurrent.GetProperty("rowVersion").GetString() },
                new { id = 999999, rowVersion = goodCurrent.GetProperty("rowVersion").GetString() },
            },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, body.GetProperty("requestedCount").GetInt32());
        Assert.Equal(1, body.GetProperty("succeededCount").GetInt32());
        Assert.Equal(2, body.GetProperty("failedCount").GetInt32());
        var reasons = body.GetProperty("failed").EnumerateArray()
            .ToDictionary(f => f.GetProperty("id").GetInt32(), f => f.GetProperty("reason").GetString());
        Assert.Equal("Conflict", reasons[stale.Id]);
        Assert.Equal("NotFound", reasons[999999]);
    }
}
