using AutoMapper;
using HopeTech.Common.Authorization;
using HopeTech.Common.Contracts;
using HopeTech.Common.Mapping;
using HopeTech.Common.Results;
using HopeTech.Common.Services;
using HopeTech.Common.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace HopeTech.Common.Tests.Services;

/// <summary>
/// <see cref="AuthService"/> in isolation: fake ports only, no ASP.NET, no DbContext,
/// no database. The mapper is real so the profile is exercised too.
/// </summary>
public class AuthServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeAccessTokenFactory _tokens = new();
    private readonly FakeClock _clock = new();

    private AuthService CreateService()
    {
        IMapper mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<UserProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();

        return new AuthService(_users, _tokens, _clock, mapper);
    }

    private static RegisterRequest Registration(
        string email = "sam@hopetech.test",
        string userName = "sam",
        string firstName = "Sam",
        string lastName = "Altman",
        string password = "Str0ng!Passw0rd") => new(email, userName, firstName, lastName, password);

    [Fact]
    public async Task Register_creates_the_account_and_returns_a_token()
    {
        var result = await CreateService().RegisterAsync(Registration(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("sam@hopetech.test", result.Value.User.Email);
        Assert.Equal("sam", result.Value.User.UserName);
        Assert.Equal("Sam", result.Value.User.FirstName);
        Assert.Equal("Altman", result.Value.User.LastName);
        Assert.NotEmpty(result.Value.AccessToken);
    }

    [Fact]
    public async Task Register_grants_the_default_user_role()
    {
        var result = await CreateService().RegisterAsync(Registration(), CancellationToken.None);

        Assert.Equal([Roles.User], _users.GrantedRoles);
        Assert.Equal([Roles.User], result.Value.User.Roles);
        Assert.Equal([Roles.User], _tokens.IssuedRoles);
    }

    [Fact]
    public async Task Register_stamps_the_account_with_the_current_time()
    {
        await CreateService().RegisterAsync(Registration(), CancellationToken.None);

        var created = await _users.FindByEmailAsync("sam@hopetech.test", CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal(_clock.UtcNow, created.CreatedAtUtc);
    }

    [Fact]
    public async Task Register_conflicts_when_the_email_is_taken()
    {
        _users.Seed("sam@hopetech.test", "someone-else", "Str0ng!Passw0rd");

        var result = await CreateService().RegisterAsync(Registration(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("auth.email_taken", result.Error.Code);
    }

    [Fact]
    public async Task Register_conflicts_when_the_username_is_taken()
    {
        _users.Seed("someone.else@hopetech.test", "sam", "Str0ng!Passw0rd");

        var result = await CreateService().RegisterAsync(Registration(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("auth.username_taken", result.Error.Code);
    }

    [Fact]
    public async Task Register_surfaces_a_rejected_password_as_a_validation_failure()
    {
        _users.CreateFailure = Error.Validation(
            new Dictionary<string, string[]> { ["Password"] = ["Passwords must have at least one digit."] });

        var result = await CreateService().RegisterAsync(
            Registration(password: "weak"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.NotNull(result.Error.Failures);
        Assert.Contains("Password", result.Error.Failures.Keys);
    }

    [Fact]
    public async Task Login_returns_a_token_for_correct_credentials()
    {
        _users.Seed("sam@hopetech.test", "sam", "Str0ng!Passw0rd", Roles.User, Roles.Moderator);

        var result = await CreateService().LoginAsync(
            new LoginRequest("sam@hopetech.test", "Str0ng!Passw0rd"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("sam", result.Value.User.UserName);
        Assert.Equal([Roles.User, Roles.Moderator], result.Value.User.Roles);
    }

    [Fact]
    public async Task Login_matches_the_email_regardless_of_case()
    {
        _users.Seed("sam@hopetech.test", "sam", "Str0ng!Passw0rd", Roles.User);

        var result = await CreateService().LoginAsync(
            new LoginRequest("SAM@HopeTech.TEST", "Str0ng!Passw0rd"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Login_does_not_accept_the_username_as_the_credential()
    {
        _users.Seed("sam@hopetech.test", "sam", "Str0ng!Passw0rd", Roles.User);

        var result = await CreateService().LoginAsync(
            new LoginRequest("sam", "Str0ng!Passw0rd"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task Login_rejects_a_wrong_password_as_unauthorized()
    {
        _users.Seed("sam@hopetech.test", "sam", "Str0ng!Passw0rd");

        var result = await CreateService().LoginAsync(
            new LoginRequest("sam@hopetech.test", "not-the-password"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task Login_cannot_be_used_to_tell_whether_an_account_exists()
    {
        _users.Seed("sam@hopetech.test", "sam", "Str0ng!Passw0rd");
        var service = CreateService();

        var wrongPassword = await service.LoginAsync(
            new LoginRequest("sam@hopetech.test", "not-the-password"),
            CancellationToken.None);

        var unknownUser = await service.LoginAsync(
            new LoginRequest("nobody@hopetech.test", "not-the-password"),
            CancellationToken.None);

        Assert.Equal(unknownUser.Error.Code, wrongPassword.Error.Code);
        Assert.Equal(unknownUser.Error.Description, wrongPassword.Error.Description);
    }

    [Fact]
    public async Task Login_does_not_issue_a_token_when_it_fails()
    {
        _users.Seed("sam@hopetech.test", "sam", "Str0ng!Passw0rd");

        await CreateService().LoginAsync(
            new LoginRequest("sam@hopetech.test", "not-the-password"),
            CancellationToken.None);

        Assert.Null(_tokens.IssuedFor);
    }
}
