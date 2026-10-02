using Microsoft.EntityFrameworkCore;
using Platform.Common.Identity;

namespace Platform.Api.Data;

/// <summary>
/// This product's EF Core context. Everything Identity needs comes from
/// <see cref="PlatformIdentityDbContext"/>; this type exists to own the product's own
/// tables and its migration history.
///
/// A product adds its tables here: a <c>DbSet</c> per aggregate, entities in
/// <c>Entities/</c>, their mappings as <c>IEntityTypeConfiguration&lt;T&gt;</c> in
/// <c>Data/Configurations/</c>. This reference host has none, which is the point.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : PlatformIdentityDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The library's Identity mappings and the seeded roles.
        base.OnModelCreating(modelBuilder);

        // This assembly's own configurations. It finds nothing today; it is the line a
        // product edits nothing about, because ApplyConfigurationsFromAssembly scans one
        // assembly and the base class already scanned the library's (D-0009).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
