using Coffeeg.Api.Dtos;
using Coffeeg.Api.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Coffeeg.Api.Controllers;

/// <summary>
/// The beverage catalogue people choose from. Read-only: these rows belong to moderators
/// (Docs/domain/user-permissions.md), and managing them is a separate slice.
///
/// Authenticated by the inherited fallback policy - the catalogue shapes a signed-in
/// person's form, and there is no reason to serve it to anonymous callers.
/// </summary>
public sealed class BeverageTypesController(IBeverageTypeService beverageTypes) : ApiControllerBase
{
    /// <summary>Every beverage type, by name.</summary>
    [HttpGet]
    [EndpointName("ListBeverageTypes")]
    [ProducesResponseType(typeof(IReadOnlyCollection<BeverageTypeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<BeverageTypeResponse>>> List(
        CancellationToken cancellationToken) =>
        Respond(await beverageTypes.ListAsync(cancellationToken));

    /// <summary>One beverage type with its ingredients, their variants and their units.</summary>
    [HttpGet("{id:int}")]
    [EndpointName("GetBeverageType")]
    [ProducesResponseType(typeof(BeverageTypeDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BeverageTypeDetailResponse>> Get(
        int id,
        CancellationToken cancellationToken) =>
        Respond(await beverageTypes.GetAsync(id, cancellationToken));
}
