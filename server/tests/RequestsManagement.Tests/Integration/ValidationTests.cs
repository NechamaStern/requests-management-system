using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RequestsManagement.Dal.Entities;

namespace RequestsManagement.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class ValidationTests(ApiFactory factory)
{
    [Theory]
    [InlineData("pageSize=1000")]
    [InlineData("pageSize=0")]
    [InlineData("page=0")]
    [InlineData("status=NotAStatus")]
    [InlineData("sortBy=Nope")]
    [InlineData("createdFrom=2024-06-30&createdTo=2024-06-01")]
    public async Task Search_InvalidParameters_Return400ProblemDetails(string query)
    {
        var response = await factory.CreateClient().GetAsync($"/api/requests?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Update_DisallowedTransition_Returns422AndChangesNothing()
    {
        var userId = await factory.ResetAsync();
        var seeded = await factory.AddRequestAsync(status: RequestStatus.New);
        var client = factory.CreateClient();
        var current = await StatusUpdateTests.GetRequestAsync(client, seeded.Id);

        // New -> Completed is not an allowed transition.
        var response = await client.PutAsJsonAsync($"/api/requests/{seeded.Id}",
            StatusUpdateTests.UpdateBody("Completed", current, userId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var after = await StatusUpdateTests.GetRequestAsync(client, seeded.Id);
        Assert.Equal("New", after.GetProperty("status").GetString());
        var history = await client.GetFromJsonAsync<JsonElement>($"/api/requests/{seeded.Id}/history");
        Assert.Equal(0, history.GetArrayLength());
    }

    [Fact]
    public async Task Update_UnknownStatusValue_Returns400()
    {
        var userId = await factory.ResetAsync();
        var seeded = await factory.AddRequestAsync();
        var client = factory.CreateClient();
        var current = await StatusUpdateTests.GetRequestAsync(client, seeded.Id);

        var response = await client.PutAsJsonAsync($"/api/requests/{seeded.Id}",
            StatusUpdateTests.UpdateBody("Banana", current, userId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_MissingRequest_Returns404()
    {
        var userId = await factory.ResetAsync();
        var seeded = await factory.AddRequestAsync();
        var client = factory.CreateClient();
        var current = await StatusUpdateTests.GetRequestAsync(client, seeded.Id);

        var response = await client.PutAsJsonAsync("/api/requests/999999",
            StatusUpdateTests.UpdateBody("InProgress", current, userId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
