using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RequestsManagement.Dal.Entities;

namespace RequestsManagement.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class StatusUpdateTests(ApiFactory factory)
{
    [Fact]
    public async Task Update_ValidTransition_ReturnsNewRowVersionAndWritesAuditHistory()
    {
        var userId = await factory.ResetAsync();
        var seeded = await factory.AddRequestAsync(status: RequestStatus.New);
        var client = factory.CreateClient();
        var current = await GetRequestAsync(client, seeded.Id);

        var response = await client.PutAsJsonAsync($"/api/requests/{seeded.Id}",
            UpdateBody("InProgress", current, userId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("InProgress", updated.GetProperty("status").GetString());
        Assert.NotEqual(current.GetProperty("rowVersion").GetString(), updated.GetProperty("rowVersion").GetString());
        Assert.True(updated.GetProperty("updatedAt").GetDateTime() >= current.GetProperty("updatedAt").GetDateTime());

        var history = await client.GetFromJsonAsync<JsonElement>($"/api/requests/{seeded.Id}/history");
        Assert.Equal(1, history.GetArrayLength());
        var row = history[0];
        Assert.Equal("New", row.GetProperty("previousStatus").GetString());
        Assert.Equal("InProgress", row.GetProperty("newStatus").GetString());
        Assert.Equal(userId, row.GetProperty("changedByUserId").GetInt32());
    }

    internal static async Task<JsonElement> GetRequestAsync(HttpClient client, int id) =>
        await client.GetFromJsonAsync<JsonElement>($"/api/requests/{id}");

    internal static object UpdateBody(string status, JsonElement current, int changedByUserId) => new
    {
        status,
        priority = current.GetProperty("priority").GetString(),
        assignedToUserId = (int?)null,
        rowVersion = current.GetProperty("rowVersion").GetString(),
        changedByUserId,
    };
}
