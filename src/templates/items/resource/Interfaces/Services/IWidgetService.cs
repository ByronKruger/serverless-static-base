using HopeTech.Api.Dtos;
using HopeTech.Common.Results;

namespace HopeTech.Api.Interfaces.Services;

/// <summary>
/// The widget use cases. One method per use case, named for the verb the caller means.
/// Every method returns a <see cref="Result"/>: an expected failure - not found, a
/// conflict, a rule the caller broke - is data, not an exception.
/// </summary>
public interface IWidgetService
{
    /// <summary>Lists widgets visible to the caller.</summary>
    Task<Result<IReadOnlyList<WidgetResponse>>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Returns one widget, or <see cref="ErrorType.NotFound"/>.</summary>
    Task<Result<WidgetResponse>> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>Creates a widget, or <see cref="ErrorType.Conflict"/>.</summary>
    Task<Result<WidgetResponse>> CreateAsync(CreateWidgetRequest request, CancellationToken cancellationToken);

    /// <summary>Deletes a widget, or <see cref="ErrorType.NotFound"/>.</summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}
