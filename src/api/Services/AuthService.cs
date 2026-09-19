using AutoMapper;
using Coffeeg.Api.Authorization;
using Coffeeg.Api.Dtos;
using Coffeeg.Api.Entities;
using Coffeeg.Api.Interfaces;
using Coffeeg.Api.Interfaces.Repositories;
using Coffeeg.Api.Interfaces.Services;
using Coffeeg.Api.MapperProfiles;
using Coffeeg.Api.Results;

namespace Coffeeg.Api.Services;

/// <inheritdoc cref="IAuthService"/>
public sealed class AuthService(
    IUserRepository users,
    IAccessTokenFactory accessTokenFactory,
    IClock clock,
    IMapper mapper) : IAuthService
{
    /// <summary>
    /// Deliberately identical for an unknown address and a wrong password. Telling the
    /// two apart would let anyone test whether an account exists for an email.
    /// </summary>
    private static Error InvalidCredentials =>
        Error.Unauthorized("auth.invalid_credentials", "The email address or password is incorrect.");

    public async Task<Result<AuthenticationResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        // Checked up front so the caller gets a precise conflict. The unique indexes on
        // email and username are what actually guarantee it under a race.
        if (await users.FindByEmailAsync(request.Email, cancellationToken) is not null)
        {
            return Error.Conflict("auth.email_taken", "That email address is already registered.");
        }

        if (await users.FindByUserNameAsync(request.UserName, cancellationToken) is not null)
        {
            return Error.Conflict("auth.username_taken", "That username is already taken.");
        }

        var user = new AppUser
        {
            Email = request.Email,
            UserName = request.UserName,
            CreatedAtUtc = clock.UtcNow
        };

        var created = await users.CreateAsync(user, request.Password, cancellationToken);
        if (created.IsFailure)
        {
            return Result.Failure<AuthenticationResponse>(created.Error);
        }

        var granted = await users.AddToRoleAsync(created.Value, Roles.User, cancellationToken);
        if (granted.IsFailure)
        {
            return Result.Failure<AuthenticationResponse>(granted.Error);
        }

        return Issue(created.Value, [Roles.User]);
    }

    public async Task<Result<AuthenticationResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        // Email is the sign-in credential (D-0009); the username is a display name.
        var user = await users.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            return InvalidCredentials;
        }

        if (!await users.CheckPasswordAsync(user, request.Password, cancellationToken))
        {
            return InvalidCredentials;
        }

        var roles = await users.GetRolesAsync(user, cancellationToken);

        return Issue(user, roles);
    }

    private AuthenticationResponse Issue(AppUser user, IReadOnlyCollection<string> roles)
    {
        var token = accessTokenFactory.Create(user, roles);
        var response = mapper.Map<UserResponse>(user, options => options.Items[UserProfile.RolesKey] = roles);

        return new AuthenticationResponse(token.Value, token.ExpiresAtUtc, response);
    }
}
