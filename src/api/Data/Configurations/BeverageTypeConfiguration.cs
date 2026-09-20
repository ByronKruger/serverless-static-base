using Coffeeg.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coffeeg.Api.Data.Configurations;

/// <summary>
/// Beverage types, seeded as reference data. A person picks from these; only a moderator
/// may add one (Docs/domain/user-permissions.md), and those screens are assumed built.
///
/// Two types are seeded rather than one so the shape is exercised end to end: coffee
/// carries both complex ingredients (milk and sugar), and whiskey carries the shots unit
/// alongside two Boolean ones (ice, water).
///
/// Coffee deliberately does not name water. It is assumed to be in there, and an
/// ingredient nobody can say anything useful about is noise on the form.
/// </summary>
public sealed class BeverageTypeConfiguration : IEntityTypeConfiguration<BeverageType>
{
    public void Configure(EntityTypeBuilder<BeverageType> builder)
    {
        builder.Property(type => type.Name).IsRequired().HasMaxLength(128);
        builder.Property(type => type.Description).HasMaxLength(512);

        builder.HasIndex(type => type.Name).IsUnique();

        builder.HasData(
            new BeverageType
            {
                Id = 1,
                Name = "Coffee",
                Description = "Brewed coffee, however you take it."
            },
            new BeverageType
            {
                Id = 2,
                Name = "Whiskey",
                Description = "Neat, on the rocks, or with a splash of water."
            });
    }
}
