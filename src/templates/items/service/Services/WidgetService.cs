using AutoMapper;
using HopeTech.Api.Dtos;
using HopeTech.Api.Interfaces.Services;
using HopeTech.Common.Abstractions;
using HopeTech.Common.Results;

namespace HopeTech.Api.Services;

/// <inheritdoc cref="IWidgetService"/>
/// <remarks>
/// Register this pair yourself - the library registers its own services and knows
/// nothing about yours:
/// <code>builder.Services.AddScoped&lt;IWidgetService, WidgetService&gt;();</code>
///
/// The shape of every method is the same: authorize the resource rule, load through a
/// port, mutate or query, map to a contract, return a <see cref="Result"/>. The
/// cancellation token is passed through every await.
///
/// Nothing here knows about ASP.NET - no <c>HttpContext</c>, no <c>ActionResult</c>, no
/// status codes - which is what lets the unit test run without a web host.
/// </remarks>
public sealed class WidgetService(
    IMapper mapper,
    IClock clock,
    ICurrentUser currentUser) : IWidgetService
{
    /// <summary>
    /// Error codes start here, as private members. A code earns a named class in
    /// <c>Results/</c> once it has a second caller, and not before.
    /// </summary>
    private static Error NotFoundError(int id) =>
        Error.NotFound("widget.not_found", $"No widget with id {id} exists.");

    private static Error NameTakenError(string name) =>
        Error.Conflict("widget.name_taken", $"A widget called '{name}' already exists.");

    public Task<Result<IReadOnlyList<WidgetResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        // TODO: load through a repository port. `dotnet new htc-entity -n Widget`
        // scaffolds the entity, its configuration, and that port.
        IReadOnlyList<WidgetResponse> widgets = [];

        return Task.FromResult(Result.Success(widgets));
    }

    public Task<Result<WidgetResponse>> GetAsync(int id, CancellationToken cancellationToken)
    {
        // TODO: load through the port, and return this error when it finds nothing. An
        // expected miss is a Result, never a thrown NotFoundException.
        return Task.FromResult(Result.Failure<WidgetResponse>(NotFoundError(id)));
    }

    public Task<Result<WidgetResponse>> CreateAsync(
        CreateWidgetRequest request,
        CancellationToken cancellationToken)
    {
        // Shape validation already happened at the edge. What belongs here is what a
        // validator cannot know: uniqueness, ownership, and any rule that needs state.
        //
        // TODO: persist through the port and map the saved entity, not the request.
        _ = clock.UtcNow;
        _ = currentUser.UserKey;
        _ = mapper;

        return Task.FromResult(Result.Failure<WidgetResponse>(NameTakenError(request.Name)));
    }

    public Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        // TODO: load, check the caller may delete it, delete, return Result.Success().
        return Task.FromResult(Result.Failure(NotFoundError(id)));
    }
}
