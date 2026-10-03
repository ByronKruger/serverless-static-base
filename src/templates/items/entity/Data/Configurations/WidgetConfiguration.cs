using HopeTech.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HopeTech.Api.Data.Configurations;

/// <summary>
/// Mapping for <see cref="Widget"/>. Found by the
/// <c>ApplyConfigurationsFromAssembly</c> call in the product's own context - the one in
/// the library's base context scans the library's assembly only, so both calls are
/// needed.
/// </summary>
public sealed class WidgetConfiguration : IEntityTypeConfiguration<Widget>
{
    public void Configure(EntityTypeBuilder<Widget> builder)
    {
        builder.ToTable("SET-THE-TABLE");

        builder.HasKey(widget => widget.Id);

        builder.Property(widget => widget.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(widget => widget.Description)
            .HasMaxLength(2000);

        // The unique index, not the service's pre-check, is what actually guarantees
        // uniqueness under a race. The pre-check exists to give a precise 409.
        builder.HasIndex(widget => widget.Name)
            .IsUnique();
    }
}
