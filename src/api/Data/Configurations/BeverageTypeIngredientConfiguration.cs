using Coffeeg.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coffeeg.Api.Data.Configurations;

/// <summary>
/// Which ingredients each beverage type is composed of.
///
/// Composition names the top-level ingredient only: coffee is composed of "Milk", not of
/// "Soy milk". A preference then chooses a variant, and
/// <see cref="Services.BeveragePreferenceService"/> walks the ancestry to decide whether
/// the choice belongs to this beverage.
/// </summary>
public sealed class BeverageTypeIngredientConfiguration : IEntityTypeConfiguration<BeverageTypeIngredient>
{
    public void Configure(EntityTypeBuilder<BeverageTypeIngredient> builder)
    {
        // Named explicitly: without a DbSet, EF would call the table BeverageTypeIngredient
        // while every other table here is plural.
        builder.ToTable("BeverageTypeIngredients");

        builder.HasKey(composition => new { composition.BeverageTypeId, composition.IngredientId });

        builder
            .HasOne(composition => composition.BeverageType)
            .WithMany(type => type.Ingredients)
            .HasForeignKey(composition => composition.BeverageTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: an ingredient still used by a beverage type cannot simply vanish.
        builder
            .HasOne(composition => composition.Ingredient)
            .WithMany(ingredient => ingredient.BeverageTypes)
            .HasForeignKey(composition => composition.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new BeverageTypeIngredient { BeverageTypeId = 1, IngredientId = 1, SortOrder = 0 },
            new BeverageTypeIngredient { BeverageTypeId = 1, IngredientId = 2, SortOrder = 1 },
            new BeverageTypeIngredient { BeverageTypeId = 1, IngredientId = 6, SortOrder = 2 },
            new BeverageTypeIngredient { BeverageTypeId = 2, IngredientId = 10, SortOrder = 0 },
            new BeverageTypeIngredient { BeverageTypeId = 2, IngredientId = 11, SortOrder = 1 },
            new BeverageTypeIngredient { BeverageTypeId = 2, IngredientId = 9, SortOrder = 2 });
    }
}
