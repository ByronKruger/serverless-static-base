using Coffeeg.Api.Dtos;
using Coffeeg.Api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coffeeg.Api.Controllers;

/// <summary>
/// Registration and sign-in. Both endpoints are anonymous by necessity - they are how a
/// caller obtains the token every other endpoint requires.
///
/// Both return 200 rather than 201: what comes back is a session, not a representation
/// of the created user, and there is no user resource to point a Location header at yet.
/// </summary>
public sealed class AuthController(IAuthService authService) : ApiControllerBase
{
    /// <summary>Creates an account and signs the new person in.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EndpointName("Register")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthenticationResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken) =>
        Respond(await authService.RegisterAsync(request, cancellationToken));

    /// <summary>Signs an existing person in with their email and password.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EndpointName("Login")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthenticationResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken) =>
        Respond(await authService.LoginAsync(request, cancellationToken));
}
