using Microsoft.EntityFrameworkCore;
using HopeTech.Common.Identity;

namespace HopeTech.Api.Data;

/// <summary>
/// This product's EF Core context. Everything Identity needs comes from
/// <see cref="HopeTechIdentityDbContext"/>; this type exists to own the product's own
/// tables and its migration history.
///
/// Add your tables here: a <c>DbSet</c> per aggregate, entities in <c>Entities/</c>, their
/// mappings as <c>IEntityTypeConfiguration&lt;T&gt;</c> in <c>Data/Configurations/</c>.
/// The library ships no migration, because two products cannot share a
/// <c>__EFMigrationsHistory</c>.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : HopeTechIdentityDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The library's Identity mappings and the seeded roles.
        base.OnModelCreating(modelBuilder);

        // This assembly's own configurations. ApplyConfigurationsFromAssembly scans one
        // assembly and the base class already scanned the library's, so both calls are
        // needed and neither is redundant.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
