using AutoMapper;
using HopeTech.Api.Dtos;
using HopeTech.Api.Interfaces.Services;
using HopeTech.Api.Results;
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
/// Nothing here knows about ASP.NET: no <c>HttpContext</c>, no <c>ActionResult</c>, no
/// status codes. That is what makes the unit test in
/// <c>tests/.../WidgetServiceTests</c> possible without a web host.
/// </remarks>
public sealed class WidgetService(
    IMapper mapper,
    IClock clock,
    ICurrentUser currentUser) : IWidgetService
{
    public Task<Result<IReadOnlyList<WidgetResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        // TODO: load through a repository port. `dotnet new htc-entity -n Widget`
        // scaffolds the entity, its configuration, and that port.
        IReadOnlyList<WidgetResponse> widgets = [];

        return Task.FromResult(Result.Success(widgets));
    }

    public Task<Result<WidgetResponse>> GetAsync(int id, CancellationToken cancellationToken)
    {
        // TODO: load through the port, and return the error below when it finds nothing.
        // An expected miss is a Result, never a thrown NotFoundException.
        return Task.FromResult(Result.Failure<WidgetResponse>(WidgetErrors.NotFound(id)));
    }

    public Task<Result<WidgetResponse>> CreateAsync(
        CreateWidgetRequest request,
        CancellationToken cancellationToken)
    {
        // Shape validation already happened at the edge, in CreateWidgetRequestValidator.
        // What belongs here is what the validator cannot know: uniqueness, ownership, and
        // any rule that needs the current state.
        //
        // TODO: persist through the port and map the saved entity, not the request.
        _ = clock.UtcNow;
        _ = currentUser.UserKey;
        _ = mapper;

        return Task.FromResult(Result.Failure<WidgetResponse>(WidgetErrors.NameTaken(request.Name)));
    }

    public Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        // TODO: load, check the caller may delete it, delete, return Result.Success().
        return Task.FromResult(Result.Failure(WidgetErrors.NotFound(id)));
    }
}
