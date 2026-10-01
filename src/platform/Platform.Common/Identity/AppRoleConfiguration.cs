using Platform.Common.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Platform.Common.Identity;

/// <summary>
/// Seeds the three roles from <see cref="Roles"/>. They are reference data, not
/// something an administrator creates, so they ship in the migration.
///
/// Every seeded value is a literal: <c>HasData</c> compares against the last migration,
/// so a generated stamp would produce a spurious model change on every scaffold.
/// </summary>
public sealed class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder) =>
        builder.HasData(
            new AppRole
            {
                Id = 1,
                Name = Roles.Administrator,
                NormalizedName = "ADMINISTRATOR",
                ConcurrencyStamp = "8f3b1a26-0f3e-4f62-9a0f-6f5d6f3a1c01"
            },
            new AppRole
            {
                Id = 2,
                Name = Roles.Moderator,
                NormalizedName = "MODERATOR",
                ConcurrencyStamp = "8f3b1a26-0f3e-4f62-9a0f-6f5d6f3a1c02"
            },
            new AppRole
            {
                Id = 3,
                Name = Roles.User,
                NormalizedName = "USER",
                ConcurrencyStamp = "8f3b1a26-0f3e-4f62-9a0f-6f5d6f3a1c03"
            });
}
