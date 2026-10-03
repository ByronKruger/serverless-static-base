using HopeTech.Api.Entities;

namespace HopeTech.Api.Interfaces.Repositories;

/// <summary>
/// Persistence for widgets, as a port. Nothing above this interface sees
/// <c>DbContext</c>, an EF entity's tracking state, or an <c>IQueryable</c> - the methods
/// return materialised results, so a caller cannot accidentally compose a query across
/// the boundary.
/// </summary>
public interface IWidgetRepository
{
    Task<IReadOnlyList<Widget>> ListAsync(CancellationToken cancellationToken);

    Task<Widget?> FindAsync(int id, CancellationToken cancellationToken);

    Task<Widget?> FindByNameAsync(string name, CancellationToken cancellationToken);

    Task<Widget> AddAsync(Widget widget, CancellationToken cancellationToken);

    Task RemoveAsync(Widget widget, CancellationToken cancellationToken);
}
