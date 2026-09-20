using Coffeeg.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coffeeg.Api.Data.Configurations;

/// <summary>
/// A person's preferences. The unique index is the rule from
/// Docs/domain/beverage-preference.md - one preference per beverage type per person -
/// expressed where it cannot be raced past. The service checks it first so the caller
/// gets a 409 with a useful code rather than a constraint violation.
///
/// Deleting a user takes their preferences with them; deleting a beverage type does not,
/// because that would quietly discard what people saved.
/// </summary>
public sealed class BeveragePreferenceConfiguration : IEntityTypeConfiguration<BeveragePreference>
{
    public void Configure(EntityTypeBuilder<BeveragePreference> builder)
    {
        builder.Property(preference => preference.CreatedAtUtc).IsRequired();

        builder
            .HasOne(preference => preference.User)
            .WithMany()
            .HasForeignKey(preference => preference.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(preference => preference.BeverageType)
            .WithMany()
            .HasForeignKey(preference => preference.BeverageTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(preference => new { preference.UserId, preference.BeverageTypeId })
            .IsUnique();
    }
}
