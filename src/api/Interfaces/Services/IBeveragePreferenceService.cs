using Coffeeg.Api.Dtos;
using Coffeeg.Api.Results;

namespace Coffeeg.Api.Interfaces.Services;

/// <summary>
/// A person's own beverage preferences. Every method acts on the caller from
/// <see cref="ICurrentUser"/>; none takes a user id, so one caller's preferences can
/// never be asked for by another.
/// </summary>
public interface IBeveragePreferenceService
{
    /// <summary>
    /// Saves how the caller likes one kind of drink. Fails as a conflict when they
    /// already have a preference for that beverage type, as not found when the type does
    /// not exist, and as a validation failure when an ingredient does not belong to the
    /// beverage, needs a variant chosen, or carries an amount its unit does not allow.
    /// </summary>
    Task<Result<BeveragePreferenceResponse>> CreateAsync(
        CreateBeveragePreferenceRequest request,
        CancellationToken cancellationToken);

    /// <summary>The caller's preferences, newest first.</summary>
    Task<Result<IReadOnlyCollection<BeveragePreferenceResponse>>> ListMineAsync(CancellationToken cancellationToken);

    /// <summary>
    /// One of the caller's preferences. Somebody else's is reported as not found rather
    /// than forbidden, so ids cannot be probed.
    /// </summary>
    Task<Result<BeveragePreferenceResponse>> GetMineAsync(int preferenceId, CancellationToken cancellationToken);
}
