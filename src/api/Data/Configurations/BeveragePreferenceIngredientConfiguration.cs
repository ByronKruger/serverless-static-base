using Coffeeg.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coffeeg.Api.Data.Configurations;

/// <summary>
/// The lines of a preference.
///
/// <c>Amount</c> is nullable on purpose: null is the domain's "none of this", and it is
/// the same statement as 0 teaspoons or <c>No</c>. Precision is fixed at (9,3) so a
/// third of a teaspoon survives a round trip and SQL Server does not pick a default.
/// </summary>
public sealed class BeveragePreferenceIngredientConfiguration
    : IEntityTypeConfiguration<BeveragePreferenceIngredient>
{
    public void Configure(EntityTypeBuilder<BeveragePreferenceIngredient> builder)
    {
        // Named explicitly for the same reason as BeverageTypeIngredients: no DbSet, so
        // EF would otherwise use the singular entity name.
        builder.ToTable("BeveragePreferenceIngredients");

        builder.Property(line => line.Amount).HasPrecision(9, 3);

        builder
            .HasOne(line => line.BeveragePreference)
            .WithMany(preference => preference.Ingredients)
            .HasForeignKey(line => line.BeveragePreferenceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(line => line.Ingredient)
            .WithMany()
            .HasForeignKey(line => line.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        // One line per ingredient: saying two different things about the same ingredient
        // in one preference is a contradiction, not a merge.
        builder
            .HasIndex(line => new { line.BeveragePreferenceId, line.IngredientId })
            .IsUnique();
    }
}
