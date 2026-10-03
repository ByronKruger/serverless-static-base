using HopeTech.Api.Data;
using HopeTech.Api.Entities;
using HopeTech.Api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HopeTech.Api.Repositories;

/// <inheritdoc cref="IWidgetRepository"/>
/// <remarks>
/// The only type in the product that is allowed to name <c>AppDbContext</c>, besides the
/// context itself and the composition root.
///
/// Three steps finish this off, none of which a template can do:
/// add <c>public DbSet&lt;Widget&gt; Widgets =&gt; Set&lt;Widget&gt;();</c> to the context,
/// register <c>builder.Services.AddScoped&lt;IWidgetRepository, WidgetRepository&gt;();</c>,
/// then <c>dotnet ef migrations add AddWidget --output-dir Data/Migrations</c>.
/// </remarks>
public sealed class WidgetRepository(AppDbContext database) : IWidgetRepository
{
    public async Task<IReadOnlyList<Widget>> ListAsync(CancellationToken cancellationToken) =>
        await database.Set<Widget>()
            .AsNoTracking()
            .OrderBy(widget => widget.Name)
            .ToListAsync(cancellationToken);

    public Task<Widget?> FindAsync(int id, CancellationToken cancellationToken) =>
        database.Set<Widget>()
            .FirstOrDefaultAsync(widget => widget.Id == id, cancellationToken);

    public Task<Widget?> FindByNameAsync(string name, CancellationToken cancellationToken) =>
        database.Set<Widget>()
            .FirstOrDefaultAsync(widget => widget.Name == name, cancellationToken);

    public async Task<Widget> AddAsync(Widget widget, CancellationToken cancellationToken)
    {
        database.Add(widget);
        await database.SaveChangesAsync(cancellationToken);

        return widget;
    }

    public async Task RemoveAsync(Widget widget, CancellationToken cancellationToken)
    {
        database.Remove(widget);
        await database.SaveChangesAsync(cancellationToken);
    }
}
