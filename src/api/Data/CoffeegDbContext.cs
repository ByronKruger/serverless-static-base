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
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity's own mappings first - the configurations below refine them.
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration<T> in Data/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoffeegDbContext).Assembly);
    }
}
