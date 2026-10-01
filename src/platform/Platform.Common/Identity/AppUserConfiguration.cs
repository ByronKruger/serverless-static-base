using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Platform.Common.Identity;

/// <summary>
/// Refines Identity's default user mapping: email becomes a required, uniquely
/// indexed natural key, and username keeps the unique index Identity already gives it.
/// </summary>
public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(user => user.Email).IsRequired();
        builder.Property(user => user.NormalizedEmail).IsRequired();
        builder.Property(user => user.UserName).IsRequired();
        builder.Property(user => user.CreatedAtUtc).IsRequired();

        // Deliberately nullable and deliberately unindexed. Required at registration but
        // not in the schema, so the accounts that predate them survive (D-0016); and
        // searched through UPPER(), which no index on the raw column could serve
        // (D-0018).
        builder.Property(user => user.FirstName).HasMaxLength(64);
        builder.Property(user => user.LastName).HasMaxLength(64);

        // Identity indexes NormalizedEmail but does not make it unique. Email is this
        // system's natural key for a person, so it must be.
        builder
            .HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique();
    }
}
