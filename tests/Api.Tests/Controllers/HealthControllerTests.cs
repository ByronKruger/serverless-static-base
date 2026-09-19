using System.Net;
using System.Net.Http.Json;
using Coffeeg.Api.Dtos;

namespace Coffeeg.Api.Tests.Controllers;

/// <summary>
/// Smoke test for the request pipeline: routing, the anonymous opt-out from the
/// default-deny fallback policy, and the camelCase JSON contract.
/// </summary>
public class HealthControllerTests(CoffeegApiFactory factory) : IClassFixture<CoffeegApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_health_is_anonymous_and_returns_ok()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_health_returns_the_documented_shape()
    {
        var health = await _client.GetFromJsonAsync<HealthResponse>("/api/health");

        Assert.NotNull(health);
        Assert.Equal("Healthy", health.Status);
        Assert.Equal("Testing", health.Environment);
    }

    [Fact]
    public async Task Unknown_route_is_challenged_rather_than_disclosed()
    {
        // The fallback policy runs before routing has an endpoint to talk about, so an
        // unauthenticated caller cannot use 404-vs-401 to probe which routes exist.
        var response = await _client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
