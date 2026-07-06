using Lumineux.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumineux.Infrastructure.Persistence.Configurations;

public sealed class AntennaConfiguration : IEntityTypeConfiguration<Antenna>
{
    public void Configure(EntityTypeBuilder<Antenna> builder)
    {
        builder.ToTable("antennas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(60).IsRequired();
        builder.Property(x => x.Label).HasColumnName("label").HasMaxLength(100).IsRequired();
        builder.Property(x => x.District).HasColumnName("district");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();

        // Unicité du code d'antenne (feature 016, FR-002) — matérialisée en base (Principe II).
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Ignore(x => x.IsActive);

        AuditColumns.Apply(builder);
    }
}
