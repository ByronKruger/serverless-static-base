using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HopeTech.Common.Identity;

/// <summary>
/// The base context every product context derives from. Brings the <c>AspNet*</c> tables
/// into the model through <see cref="IdentityDbContext{TUser,TRole,TKey}"/> with an
/// <see cref="int"/> key, and nothing else: a product's own tables are the product's
/// business.
///
/// Deliberately holds no <c>DbSet</c>. Nothing here is ever an API contract.
/// </summary>
public abstract class HopeTechIdentityDbContext(DbContextOptions options)
    : IdentityDbContext<AppUser, AppRole, int>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity's own mappings first - the configurations below refine them.
        base.OnModelCreating(modelBuilder);

        // This assembly's configurations: the unique email index, and the three seeded
        // roles. ApplyConfigurationsFromAssembly scans exactly one assembly, so a derived
        // context calling it for its own assembly does not pick these up - which is why
        // the call is here rather than left to the product (Docs/DECISIONS.md D-0009).
        // Miss it and the roles disappear from the model, which surfaces as registration
        // failing on a role that does not exist.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HopeTechIdentityDbContext).Assembly);
    }
}
