using Coffeeg.Api.Entities;
using Coffeeg.Api.Interfaces.Repositories;

namespace Coffeeg.Application.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IBeverageTypeRepository"/> over <see cref="BeverageCatalogue"/>.
/// The graph is always fully loaded, which is what the real repository's includes
/// guarantee - so a service test exercises the same shape the database hands back.
/// </summary>
public sealed class FakeBeverageTypeRepository : IBeverageTypeRepository
{
    private readonly List<BeverageType> _types = BeverageCatalogue.Build();

    /// <summary>The graph the fake serves, for tests that need to reach into it.</summary>
    public IReadOnlyList<BeverageType> Types => _types;

    public Task<IReadOnlyCollection<BeverageType>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<BeverageType>>(
            _types.OrderBy(type => type.Name, StringComparer.Ordinal).ToList());

    public Task<BeverageType?> FindWithIngredientsAsync(int beverageTypeId, CancellationToken cancellationToken) =>
        Task.FromResult(_types.SingleOrDefault(type => type.Id == beverageTypeId));
}
