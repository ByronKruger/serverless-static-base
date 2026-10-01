using Platform.Common.Contracts;
using Platform.Common.Abstractions;
using Platform.Common.Http;
using Microsoft.AspNetCore.Mvc;

namespace Platform.Api.Controllers;

/// <summary>
/// Finding other people.
///
/// Authenticated by the inherited fallback policy, with no named policy on the action:
/// any signed-in person may look for anybody, so there is no role to check and no
/// ownership rule to apply.
///
/// No response carries an email. Email is the sign-in credential, so returning it would
/// let anybody confirm which addresses are registered - and
/// <c>UsersControllerTests</c> asserts the absence against the raw JSON, because a DTO
/// assertion only proves what the DTO declares.
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
}
