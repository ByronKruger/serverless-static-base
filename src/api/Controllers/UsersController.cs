using Coffeeg.Api.Dtos;
using Coffeeg.Api.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Coffeeg.Api.Controllers;

/// <summary>
/// Other people: finding them, and reading one.
///
/// Authenticated by the inherited fallback policy, with no named policy on either action.
/// Any signed-in person may read any profile (D-0017), so there is no role to check and
/// no ownership rule to apply - which is exactly why this controller, unlike
/// <see cref="BeveragePreferencesController"/>, takes a user id in the route.
///
/// Neither response carries an email.
/// </summary>
public sealed class UsersController(IUserService users) : ApiControllerBase
{
    /// <summary>Autocomplete matches on username, first name or surname.</summary>
    [HttpGet]
    [EndpointName("SearchUsers")]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<UserSummaryResponse>>> Search(
        [FromQuery] UserSearchRequest request,
        CancellationToken cancellationToken) =>
        Respond(await users.SearchAsync(request, cancellationToken));

    /// <summary>One person, with the preferences they have saved.</summary>
    [HttpGet("{id:int}")]
    [EndpointName("GetUserProfile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> Get(
        int id,
        CancellationToken cancellationToken) =>
        Respond(await users.GetProfileAsync(id, cancellationToken));
}
