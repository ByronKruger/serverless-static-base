using Coffeeg.Api.Dtos;
using Coffeeg.Api.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Coffeeg.Api.Controllers;

/// <summary>
/// A person's own beverage preferences.
///
/// No action takes a user id and there is no named policy beyond the inherited
/// default-deny: the rule here is ownership, not a role, so the service applies it from
/// <c>ICurrentUser</c> (Docs/ARCHITECTURE.md). Somebody else's preference comes back as
/// 404 rather than 403, which is what stops ids being probed.
/// </summary>
public sealed class BeveragePreferencesController(IBeveragePreferenceService preferences) : ApiControllerBase
{
    /// <summary>The caller's preferences, newest first.</summary>
    [HttpGet]
    [EndpointName("ListBeveragePreferences")]
    [ProducesResponseType(typeof(IReadOnlyCollection<BeveragePreferenceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<BeveragePreferenceResponse>>> List(
        CancellationToken cancellationToken) =>
        Respond(await preferences.ListMineAsync(cancellationToken));

    /// <summary>One of the caller's preferences. Also the target of the Location header on create.</summary>
    [HttpGet("{id:int}")]
    [EndpointName("GetBeveragePreference")]
    [ProducesResponseType(typeof(BeveragePreferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BeveragePreferenceResponse>> Get(
        int id,
        CancellationToken cancellationToken) =>
        Respond(await preferences.GetMineAsync(id, cancellationToken));

    /// <summary>Saves how the caller likes one kind of drink.</summary>
    [HttpPost]
    [EndpointName("CreateBeveragePreference")]
    [ProducesResponseType(typeof(BeveragePreferenceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BeveragePreferenceResponse>> Create(
        CreateBeveragePreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var created = await preferences.CreateAsync(request, cancellationToken);

        // The route value can only be read from a success, and RespondCreated is given
        // the whole result so a failure still maps through the one translation path.
        return RespondCreated(
            created,
            nameof(Get),
            new { id = created.IsSuccess ? created.Value.Id : 0 });
    }
}
