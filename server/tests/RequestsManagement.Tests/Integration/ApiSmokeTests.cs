using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RequestsManagement.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class ApiSmokeTests(ApiFactory factory)
{
    [Fact]
    public async Task Search_ReturnsPersistedRequestFromTheDatabase()
    {
        await factory.ResetAsync();
        var seeded = await factory.AddRequestAsync(title: "Smoke test request");

        var response = await factory.CreateClient().GetAsync("/api/requests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
        Assert.Equal(seeded.Id, body.GetProperty("items")[0].GetProperty("id").GetInt32());
    }
}
