using AutoMapper;
using Platform.Common.Contracts;
using Platform.Common.Mapping;
using Platform.Common.Services;
using Platform.Application.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Platform.Application.Tests.Services;

/// <summary>
/// <see cref="UserService"/> in isolation: fake ports only, no ASP.NET, no DbContext, no
/// database.
///
/// What these tests cannot prove: <see cref="FakeUserRepository.SearchAsync"/> restates
/// the predicate rather than running it, and the API tests use the in-memory provider, so
/// nothing here exercises the SQL translation. Only a run against a real database does
/// that, so do it when you touch the predicate.
/// </summary>
public class UserServiceTests
{
    private const int Caller = 1;

    private readonly FakeUserRepository _users = new();

    private UserService CreateService(int? callerKey = Caller)
    {
        IMapper mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<UserProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();

        return new UserService(_users, new FakeCurrentUser(callerKey), mapper);
    }

    private static UserSearchRequest Search(string term, int? limit = null) =>
        new() { Term = term, Limit = limit };

    private async Task<IReadOnlyCollection<UserSummaryResponse>> SearchAsync(
        string term,
        int? limit = null,
        int? callerKey = Caller)
    {
        var result = await CreateService(callerKey).SearchAsync(Search(term, limit), CancellationToken.None);

        Assert.True(result.IsSuccess);

        return result.Value;
    }

    /// <summary>
    /// The caller is seeded first so they hold <see cref="Caller"/>, which every search
    /// then excludes.
    /// </summary>
    private void SeedCaller() => _users.SeedPerson("me", "Me", "Myself");

    [Fact]
    public async Task Search_matches_on_the_username()
    {
        SeedCaller();
        _users.SeedPerson("ada-l", "Augusta", "Byron");

        var matches = await SearchAsync("ada");

        Assert.Equal(["ada-l"], matches.Select(match => match.UserName));
    }

    [Fact]
    public async Task Search_matches_on_the_first_name()
    {
        SeedCaller();
        _users.SeedPerson("countess", "Augusta", "Byron");

        var matches = await SearchAsync("augus");

        Assert.Equal(["countess"], matches.Select(match => match.UserName));
    }

    [Fact]
    public async Task Search_matches_on_the_surname()
    {
        SeedCaller();
        _users.SeedPerson("countess", "Augusta", "Byron");

        var matches = await SearchAsync("byro");

        Assert.Equal(["countess"], matches.Select(match => match.UserName));
    }

    [Theory]
    [InlineData("AUGUSTA")]
    [InlineData("augusta")]
    [InlineData("AuGuSta")]
    public async Task Search_ignores_the_case_of_the_term(string term)
    {
        SeedCaller();
        _users.SeedPerson("countess", "Augusta", "Byron");

        var matches = await SearchAsync(term);

        Assert.Single(matches);
    }

    [Fact]
    public async Task Search_matches_the_middle_of_a_name_not_only_its_start()
    {
        SeedCaller();
        _users.SeedPerson("countess", "Augusta", "Byron");

        var matches = await SearchAsync("gust");

        Assert.Single(matches);
    }

    [Fact]
    public async Task Search_finds_an_account_that_has_no_names()
    {
        // The accounts that predate D-0016. Nullable columns mean the predicate has to
        // tolerate a null name rather than throwing or matching nothing.
        SeedCaller();
        _users.SeedPerson("grace", firstName: null, lastName: null);

        var matches = await SearchAsync("grac");

        var match = Assert.Single(matches);

        Assert.Equal("grace", match.UserName);
        Assert.Null(match.FirstName);
        Assert.Null(match.LastName);
    }

    [Fact]
    public async Task Search_leaves_the_caller_out_of_their_own_results()
    {
        SeedCaller();
        _users.SeedPerson("me-too", "Me", "Also");

        // "me" matches the caller's username and first name as well as the other account.
        var matches = await SearchAsync("me");

        Assert.Equal(["me-too"], matches.Select(match => match.UserName));
    }

    [Fact]
    public async Task Search_without_an_identifiable_caller_excludes_nobody()
    {
        // Cannot happen behind the default-deny pipeline; asserted so the `?? NoUser`
        // fallback is a documented choice rather than an accident.
        SeedCaller();

        var matches = await SearchAsync("me", callerKey: null);

        Assert.Equal(["me"], matches.Select(match => match.UserName));
    }

    [Fact]
    public async Task Search_returns_an_empty_success_when_nobody_matches()
    {
        SeedCaller();
        _users.SeedPerson("ada-l", "Augusta", "Byron");

        var matches = await SearchAsync("zzz");

        // A search for nobody is an answer, not a miss.
        Assert.Empty(matches);
    }

    [Fact]
    public async Task Search_defaults_to_ten_matches()
    {
        SeedCaller();

        for (var index = 0; index < 15; index++)
        {
            _users.SeedPerson($"person-{index:00}", "Test", "Person");
        }

        var matches = await SearchAsync("person");

        Assert.Equal(UserSearchRequest.DefaultLimit, matches.Count);
    }

    [Fact]
    public async Task Search_honours_a_supplied_limit()
    {
        SeedCaller();

        for (var index = 0; index < 15; index++)
        {
            _users.SeedPerson($"person-{index:00}", "Test", "Person");
        }

        var matches = await SearchAsync("person", limit: 3);

        Assert.Equal(3, matches.Count);
    }

    [Fact]
    public async Task Search_puts_a_username_prefix_match_before_a_mid_string_one()
    {
        // Deterministic ordering is what stops the dropdown reshuffling between
        // keystrokes.
        SeedCaller();
        _users.SeedPerson("zoe-ada", "Zoe", "Adams");
        _users.SeedPerson("ada-l", "Augusta", "Byron");

        var matches = await SearchAsync("ada");

        Assert.Equal(["ada-l", "zoe-ada"], matches.Select(match => match.UserName));
    }

    [Fact]
    public async Task Search_never_exposes_an_email()
    {
        SeedCaller();
        _users.SeedPerson("ada-l", "Augusta", "Byron");

        var matches = await SearchAsync("ada");

        // The DTO has no email property at all, which is the point - this asserts the
        // shape so adding one has to break a test.
        Assert.DoesNotContain(
            nameof(UserResponse.Email),
            typeof(UserSummaryResponse).GetProperties().Select(property => property.Name));
        Assert.Single(matches);
    }
}
