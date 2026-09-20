using Coffeeg.Api.Dtos;
using Coffeeg.Api.Results;

namespace Coffeeg.Api.Interfaces.Services;

/// <summary>
/// Reading the beverage catalogue. There is no create or update here: moderators own
/// these rows (Docs/domain/user-permissions.md) and their management is a separate slice.
/// </summary>
public interface IBeverageTypeService
{
    /// <summary>Every beverage type, for the picker on the create-preference page.</summary>
    Task<Result<IReadOnlyCollection<BeverageTypeResponse>>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// One beverage type with its ingredients, their variants and their units - enough
    /// to render a form. Fails as not found when there is no such type.
    /// </summary>
    Task<Result<BeverageTypeDetailResponse>> GetAsync(int beverageTypeId, CancellationToken cancellationToken);
}
