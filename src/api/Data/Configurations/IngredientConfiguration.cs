using Coffeeg.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coffeeg.Api.Data.Configurations;

/// <summary>
/// Ingredients, seeded as reference data alongside <see cref="UnitTypeConfiguration"/>.
///
/// The self-reference is what makes an ingredient simple or complex: milk (2) is a
/// category whose variants are low-fat (3), raw (4) and soy (5), while coffee (1) stands
/// alone. A variant is measured in the same unit as its parent, by convention rather than
/// by constraint - the model only requires that every ingredient names some unit.
///
/// Both relationships restrict on delete. A cascade from <see cref="UnitType"/> would let
/// removing a unit silently delete ingredients, and a cascade down the self-reference
/// would take a category's variants with it; a moderator should have to deal with the
/// dependants explicitly.
/// </summary>
public sealed class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> builder)
    {
        builder.Property(ingredient => ingredient.Name).IsRequired().HasMaxLength(128);
        builder.Property(ingredient => ingredient.Description).HasMaxLength(512);

        builder
            .HasOne(ingredient => ingredient.UnitType)
            .WithMany(unit => unit.Ingredients)
            .HasForeignKey(ingredient => ingredient.UnitTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(ingredient => ingredient.ParentIngredient)
            .WithMany(ingredient => ingredient.Variants)
            .HasForeignKey(ingredient => ingredient.ParentIngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique per level, so "Milk" may exist once at the top and a variant named
        // "Milk" could never shadow it.
        builder.HasIndex(ingredient => new { ingredient.ParentIngredientId, ingredient.Name }).IsUnique();

        builder.HasData(
            new Ingredient { Id = 1, Name = "Coffee", Description = "Ground coffee.", UnitTypeId = 1 },
            new Ingredient { Id = 2, Name = "Milk", Description = "Pick which milk.", UnitTypeId = 2 },
            new Ingredient { Id = 3, Name = "Low-fat milk", ParentIngredientId = 2, UnitTypeId = 2 },
            new Ingredient { Id = 4, Name = "Raw milk", ParentIngredientId = 2, UnitTypeId = 2 },
            new Ingredient { Id = 5, Name = "Soy milk", ParentIngredientId = 2, UnitTypeId = 2 },
            new Ingredient { Id = 6, Name = "Sugar", Description = "Pick which sweetener.", UnitTypeId = 1 },
            new Ingredient { Id = 7, Name = "White sugar", ParentIngredientId = 6, UnitTypeId = 1 },
            new Ingredient { Id = 8, Name = "Honey", ParentIngredientId = 6, UnitTypeId = 1 },
            // Yes/no rather than a volume: water is the optional splash in a whiskey, and
            // coffee no longer names it at all - it is assumed to be in there.
            new Ingredient { Id = 9, Name = "Water", Description = "With or without a splash.", UnitTypeId = 3 },
            new Ingredient { Id = 10, Name = "Whiskey", UnitTypeId = 4 },
            new Ingredient { Id = 11, Name = "Ice", Description = "With or without.", UnitTypeId = 3 });
    }
}
