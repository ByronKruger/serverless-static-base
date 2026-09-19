using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Coffeeg.Api.Authorization;
using Coffeeg.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Coffeeg.Api.Tests.Controllers;

/// <summary>
/// Registration and sign-in over HTTP, through the real pipeline: binding, the
/// validation filter, Problem Details, and the token the bearer handler must accept.
/// </summary>
public class AuthControllerTests(CoffeegApiFactory factory) : IClassFixture<CoffeegApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static RegisterRequest Registration(string discriminator) =>
        new($"{discriminator}@coffeeg.test", discriminator, "Str0ng!Passw0rd");

    [Fact]
    public async Task Register_returns_the_new_user_and_a_token()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", Registration("ada"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();

        Assert.NotNull(body);
        Assert.Equal("ada@coffeeg.test", body.User.Email);
        Assert.Equal("ada", body.User.UserName);
        Assert.Equal([Roles.User], body.User.Roles);
        Assert.NotEmpty(body.AccessToken);
        Assert.True(body.ExpiresAtUtc > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Registered_user_can_log_in_and_use_the_token()
    {
        await _client.PostAsJsonAsync("/api/auth/register", Registration("grace"));

        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("grace@coffeeg.test", "Str0ng!Passw0rd"));

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var body = await login.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(body);

        // The token the API issued must satisfy the bearer handler it configured. /api/health
        // is anonymous, so a bad token would fail loudly rather than being ignored.
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/does-not-exist");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);

        var authorized = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, authorized.StatusCode);
    }

    [Fact]
    public async Task Register_rejects_a_duplicate_email_with_a_conflict()
    {
        await _client.PostAsJsonAsync("/api/auth/register", Registration("linus"));

        var duplicate = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("linus@coffeeg.test", "linus-two", "Str0ng!Passw0rd"));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("application/problem+json", duplicate.Content.Headers.ContentType?.MediaType);

        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal("auth.email_taken", problem.Extensions["code"]?.ToString());
        Assert.NotNull(problem.Extensions["traceId"]);
    }

    [Fact]
    public async Task Register_rejects_a_duplicate_username_with_a_conflict()
    {
        await _client.PostAsJsonAsync("/api/auth/register", Registration("dennis"));

        var duplicate = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("dennis.other@coffeeg.test", "dennis", "Str0ng!Passw0rd"));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Register_rejects_a_malformed_email_at_the_edge()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("not-an-email", "barbara", "Str0ng!Passw0rd"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // The edge validation filter must produce the same media type as every other
        // error, not the plain application/json that ObjectResult defaults to.
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains(nameof(RegisterRequest.Email), problem.Errors.Keys);
    }

    [Fact]
    public async Task Register_surfaces_the_identity_password_policy_as_field_errors()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("weak@coffeeg.test", "weakling", "weak"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains(nameof(RegisterRequest.Password), problem.Errors.Keys);
    }

    [Fact]
    public async Task Login_with_an_unknown_user_is_unauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("nobody-at-all@coffeeg.test", "Str0ng!Passw0rd"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_takes_the_email_not_the_username()
    {
        await _client.PostAsJsonAsync("/api/auth/register", Registration("alan"));

        var byEmail = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("alan@coffeeg.test", "Str0ng!Passw0rd"));

        var byUserName = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("alan", "Str0ng!Passw0rd"));

        Assert.Equal(HttpStatusCode.OK, byEmail.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, byUserName.StatusCode);
    }

    [Fact]
    public async Task Login_requires_a_password()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("someone@coffeeg.test", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
