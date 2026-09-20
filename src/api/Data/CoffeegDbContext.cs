using Coffeeg.Api.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Coffeeg.Api.Data;

/// <summary>
/// Code-first EF Core context. Entities live in <c>Entities/</c> and their mappings in
/// <c>Data/Configurations/</c>; nothing here is ever an API contract.
///
/// Derives from <see cref="IdentityDbContext{TUser,TRole,TKey}"/> with an
/// <see cref="int"/> key, which brings the <c>AspNet*</c> tables into the model.
/// </summary>
public class CoffeegDbContext(DbContextOptions<CoffeegDbContext> options)
    : IdentityDbContext<AppUser, AppRole, int>(options)
{
    /// <summary>Beverage types a person may hold a preference for. Moderator-owned reference data.</summary>
    public DbSet<BeverageType> BeverageTypes => Set<BeverageType>();

    /// <summary>Ingredients, simple and complex. Moderator-owned reference data.</summary>
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();

    /// <summary>Units an ingredient can be measured in. Moderator-owned reference data.</summary>
    public DbSet<UnitType> UnitTypes => Set<UnitType>();

    /// <summary>Saved preferences. Private to the person who created them.</summary>
    public DbSet<BeveragePreference> BeveragePreferences => Set<BeveragePreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity's own mappings first - the configurations below refine them.
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration<T> in Data/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoffeegDbContext).Assembly);
    }
}
