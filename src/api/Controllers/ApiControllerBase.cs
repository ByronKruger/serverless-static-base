using Coffeeg.Api.Extensions;
using Coffeeg.Api.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coffeeg.Api.Controllers;

/// <summary>
/// Base for every controller: authorized by default, JSON only, and the only
/// sanctioned way to turn a <see cref="Result"/> into a response. Actions stay
/// thin - bind, call the service, translate.
/// </summary>
/// <remarks>
/// Deliberately no <c>[Produces("application/json")]</c>: that attribute is a result
/// filter that overwrites <c>ContentTypes</c> on every <c>ObjectResult</c>, which would
/// re-stamp error responses as <c>application/json</c> and break the RFC 9457 media
/// type. System.Text.Json is the only registered formatter, so responses are JSON
/// regardless, and <c>[ProducesResponseType]</c> still documents each one.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>200 with the value, or the mapped problem response.</summary>
    protected ActionResult<TValue> Respond<TValue>(Result<TValue> result) =>
        result.ToActionResult(this);

    /// <summary>204, or the mapped problem response.</summary>
    protected ActionResult Respond(Result result) =>
        result.ToActionResult(this);

    /// <summary>201 with a Location header, or the mapped problem response.</summary>
    protected ActionResult<TValue> RespondCreated<TValue>(
        Result<TValue> result,
        string actionName,
        object routeValues) =>
        result.ToCreatedResult(this, actionName, routeValues);
}
