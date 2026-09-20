using AutoMapper;
using Coffeeg.Api.Dtos;
using Coffeeg.Api.Interfaces.Repositories;
using Coffeeg.Api.Interfaces.Services;
using Coffeeg.Api.Results;

namespace Coffeeg.Api.Services;

/// <inheritdoc cref="IBeverageTypeService"/>
public sealed class BeverageTypeService(IBeverageTypeRepository beverageTypes, IMapper mapper) : IBeverageTypeService
{
    public async Task<Result<IReadOnlyCollection<BeverageTypeResponse>>> ListAsync(
        CancellationToken cancellationToken)
    {
        var types = await beverageTypes.ListAsync(cancellationToken);

        return Result.Success(mapper.Map<IReadOnlyCollection<BeverageTypeResponse>>(types));
    }

    public async Task<Result<BeverageTypeDetailResponse>> GetAsync(
        int beverageTypeId,
        CancellationToken cancellationToken)
    {
        var beverageType = await beverageTypes.FindWithIngredientsAsync(beverageTypeId, cancellationToken);
        if (beverageType is null)
        {
            return BeverageErrors.BeverageTypeNotFound(beverageTypeId);
        }

        return mapper.Map<BeverageTypeDetailResponse>(beverageType);
    }
}
