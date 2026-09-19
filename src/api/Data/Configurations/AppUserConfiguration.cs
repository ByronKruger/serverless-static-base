using Coffeeg.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coffeeg.Api.Data.Configurations;

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

        // Identity indexes NormalizedEmail but does not make it unique. Email is this
        // system's natural key for a person, so it must be.
        builder
            .HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique();
    }
}
