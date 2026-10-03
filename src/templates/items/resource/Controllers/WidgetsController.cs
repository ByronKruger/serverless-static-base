using HopeTech.Api.Dtos;
using HopeTech.Api.Interfaces.Services;
using HopeTech.Common.Authorization;
using HopeTech.Common.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HopeTech.Api.Controllers;

/// <summary>
/// The <c>widgets</c> resource. Authorized by default through
/// <see cref="ApiControllerBase"/>, and routed from the controller name: the
/// <c>[controller]</c> token is kebab-cased, so this answers on <c>api/widgets</c>.
///
/// Actions bind, call the service, and translate. No business logic, no
/// <c>DbContext</c>, no EF entities, no <c>try/catch</c> for expected outcomes.
/// </summary>
public sealed class WidgetsController(IWidgetService widgets) : ApiControllerBase
{
    /// <summary>Lists widgets visible to the caller.</summary>
    [HttpGet]
    [EndpointName("ListWidgets")]
    [ProducesResponseType(typeof(IReadOnlyList<WidgetResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WidgetResponse>>> List(
        CancellationToken cancellationToken) =>
        Respond(await widgets.ListAsync(cancellationToken));

    /// <summary>Returns one widget.</summary>
    [HttpGet("{id:int}")]
    [EndpointName("GetWidget")]
    [ProducesResponseType(typeof(WidgetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WidgetResponse>> Get(
        int id,
        CancellationToken cancellationToken) =>
        Respond(await widgets.GetAsync(id, cancellationToken));

    /// <summary>Creates a widget.</summary>
    [HttpPost]
    [Authorize(Policies.RequireModerator)]
    [EndpointName("CreateWidget")]
    [ProducesResponseType(typeof(WidgetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WidgetResponse>> Create(
        CreateWidgetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await widgets.CreateAsync(request, cancellationToken);

        // 201 plus a Location header pointing at Get. The route values have to match
        // that action's parameters, which is why the id is named here.
        return RespondCreated(result, nameof(Get), new { id = result.IsSuccess ? result.Value.Id : 0 });
    }

    /// <summary>Deletes a widget.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policies.RequireModerator)]
    [EndpointName("DeleteWidget")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(
        int id,
        CancellationToken cancellationToken) =>
        Respond(await widgets.DeleteAsync(id, cancellationToken));
}
