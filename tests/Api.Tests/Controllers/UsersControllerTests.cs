using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Platform.Common.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Platform.Api.Tests.Controllers;

/// <summary>
/// Searching for people over HTTP, through the real pipeline: query-string binding, the
/// validation filter, Problem Details, and the default-deny fallback policy.
///
/// The factory's in-memory database is shared across the class, so every account is named
/// with a <c>us-</c> prefix and searches use terms specific enough not to collide with the
/// accounts other test classes register in their own factories.
/// </summary>
public class UsersControllerTests(PlatformApiFactory factory) : IClassFixture<PlatformApiFactory>
{
    private readonly PlatformApiFactory _factory = factory;

    private async Task<(HttpClient Client, int UserId)> SignedInAsync(
        string userName,
        string firstName,
        string lastName)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest($"{userName}@platform.test", userName, firstName, lastName, "Str0ng!Passw0rd"));

        var session = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);

        return (client, session.User.Id);
    }

    /// <summary>
    /// Whether a field was reported, ignoring the case of the key.
    ///
    /// The casing is genuinely not fixed, and it is worth knowing why:
    /// <c>ModelStateDictionary</c> is case-insensitive, so when
    /// <c>FluentValidationFilter</c> adds a failure under the property name, it lands on
    /// whatever key the model binder already recorded if the two differ only by case.
    /// <c>Term</c> binds from <c>q</c>, so the binder's key does not collide and
    /// <c>"Term"</c> survives; <c>Limit</c> binds from <c>limit</c>, so the binder's
    /// lower-case key wins. The web client reads these through <c>fieldErrorsFor</c>,
    /// which is case-insensitive for exactly this reason.
    /// </summary>
    private static bool Reports(ValidationProblemDetails problem, string field) =>
        problem.Errors.Keys.Any(key => string.Equals(key, field, StringComparison.OrdinalIgnoreCase));

    private static async Task<List<UserSummaryResponse>> MatchesAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync($"/api/users?{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var matches = await response.Content.ReadFromJsonAsync<List<UserSummaryResponse>>();

        Assert.NotNull(matches);

        return matches;
    }

    [Fact]
    public async Task Searching_requires_authentication()
    {
        var response = await _factory.CreateClient().GetAsync("/api/users?q=ada");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_finds_a_person_by_username_first_name_or_surname()
    {
        var (client, _) = await SignedInAsync("us-seeker-a", "Seeker", "One");
        await SignedInAsync("us-lovelace", "Augusta", "Kowalski");

        var byUserName = await MatchesAsync(client, "q=lovelace");
        var byFirstName = await MatchesAsync(client, "q=augus");
        var bySurname = await MatchesAsync(client, "q=kowal");

        Assert.Contains(byUserName, match => match.UserName == "us-lovelace");
        Assert.Contains(byFirstName, match => match.UserName == "us-lovelace");
        Assert.Contains(bySurname, match => match.UserName == "us-lovelace");
    }

    [Fact]
    public async Task Search_ignores_case()
    {
        var (client, _) = await SignedInAsync("us-seeker-b", "Seeker", "Two");
        await SignedInAsync("us-hopper", "Grace", "Zimmerman");

        var matches = await MatchesAsync(client, "q=ZIMMER");

        Assert.Contains(matches, match => match.UserName == "us-hopper");
    }

    [Fact]
    public async Task Search_excludes_the_caller()
    {
        var (client, _) = await SignedInAsync("us-narcissus", "Narcissus", "Alone");

        var matches = await MatchesAsync(client, "q=narcis");

        Assert.DoesNotContain(matches, match => match.UserName == "us-narcissus");
    }

    [Fact]
    public async Task Search_returns_an_empty_array_when_nobody_matches()
    {
        var (client, _) = await SignedInAsync("us-seeker-c", "Seeker", "Three");

        var matches = await MatchesAsync(client, "q=nobodyatallmatchesthis");

        Assert.Empty(matches);
    }

    [Fact]
    public async Task Search_rejects_a_single_character_term_at_the_edge()
    {
        var (client, _) = await SignedInAsync("us-seeker-d", "Seeker", "Four");

        var response = await client.GetAsync("/api/users?q=a");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        // Proves the query bound as one complex object: a loose [FromQuery] scalar would
        // never reach the validator at all, so this would be a 200 with everybody in it.
        Assert.NotNull(problem);
        Assert.True(Reports(problem, nameof(UserSearchRequest.Term)));
    }

    [Fact]
    public async Task Search_rejects_a_missing_term()
    {
        var (client, _) = await SignedInAsync("us-seeker-e", "Seeker", "Five");

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(26)]
    public async Task Search_rejects_a_limit_outside_its_bounds(int limit)
    {
        var (client, _) = await SignedInAsync($"us-limit-{limit}", "Limit", "Tester");

        var response = await client.GetAsync($"/api/users?q=limit&limit={limit}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.True(Reports(problem, nameof(UserSearchRequest.Limit)));
    }

    [Fact]
    public async Task Search_honours_a_limit_within_bounds()
    {
        var (client, _) = await SignedInAsync("us-seeker-f", "Seeker", "Six");

        await SignedInAsync("us-crowd-one", "Crowded", "Person");
        await SignedInAsync("us-crowd-two", "Crowded", "Person");
        await SignedInAsync("us-crowd-three", "Crowded", "Person");

        var matches = await MatchesAsync(client, "q=us-crowd&limit=2");

        Assert.Equal(2, matches.Count);
    }

    [Fact]
    public async Task Search_never_discloses_an_email()
    {
        // Asserted on the raw body rather than through a DTO, because a DTO only shows
        // what it declares. Email is the sign-in credential (D-0009): a response carrying
        // one would let anybody confirm which addresses are registered.
        var (client, _) = await SignedInAsync("us-privacy", "Privacy", "Watcher");
        await SignedInAsync("us-private", "Private", "Person");

        var search = await client.GetStringAsync("/api/users?q=us-private");

        Assert.DoesNotContain("@platform.test", search, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", search, StringComparison.OrdinalIgnoreCase);

        // The body is not empty, so the assertions above mean something.
        Assert.Contains("us-private", search, StringComparison.Ordinal);
    }
}
