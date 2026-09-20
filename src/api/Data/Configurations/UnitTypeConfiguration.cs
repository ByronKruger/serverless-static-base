using Coffeeg.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coffeeg.Api.Data.Configurations;

/// <summary>
/// Units of measure, seeded as reference data. Moderators own this table
/// (Docs/domain/user-permissions.md); their management screens are assumed built, so the
/// rows ship in the migration the way the roles in <see cref="AppRoleConfiguration"/> do.
///
/// Every value is a literal for the same reason as the roles: <c>HasData</c> diffs against
/// the last migration, so anything generated would look like a model change each scaffold.
///
/// The five below cover the units named in Docs/domain/beverage-preference.md. Adding a
/// sixth is a row, never a code change - no part of the API names a unit.
/// </summary>
public sealed class UnitTypeConfiguration : IEntityTypeConfiguration<UnitType>
{
    public void Configure(EntityTypeBuilder<UnitType> builder)
    {
        builder.Property(unit => unit.Name).IsRequired().HasMaxLength(64);
        builder.Property(unit => unit.Symbol).HasMaxLength(16);
        builder.Property(unit => unit.Kind).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(unit => unit.MinValue).HasPrecision(9, 3);
        builder.Property(unit => unit.MaxValue).HasPrecision(9, 3);

        builder.HasIndex(unit => unit.Name).IsUnique();

        builder.HasData(
            new UnitType
            {
                Id = 1,
                Name = "Teaspoons",
                Symbol = "tsp",
                Kind = UnitKind.Numeric,
                MinValue = 0m,
                MaxValue = 20m
            },
            new UnitType
            {
                Id = 2,
                Name = "Percentage",
                Symbol = "%",
                Kind = UnitKind.Numeric,
                MinValue = 0m,
                MaxValue = 100m
            },
            // The only Boolean unit: an amount is presence, so 1 is the single legal
            // value and null means "No". See BeveragePreferenceService.ValidateAmount.
            new UnitType
            {
                Id = 3,
                Name = "Yes / no",
                Symbol = null,
                Kind = UnitKind.Boolean,
                MinValue = 1m,
                MaxValue = 1m
            },
            new UnitType
            {
                Id = 4,
                Name = "Shots",
                Symbol = "shot",
                Kind = UnitKind.Numeric,
                MinValue = 0m,
                MaxValue = 10m
            },
            new UnitType
            {
                Id = 5,
                Name = "Millilitres",
                Symbol = "ml",
                Kind = UnitKind.Numeric,
                MinValue = 0m,
                MaxValue = 1000m
            });
    }
}
