using Lumineux.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumineux.Infrastructure.Persistence.Configurations;

public sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("members");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();

        // T012a — antenne d'origine du membre (FR-011).
        builder.Property(x => x.AntennaId).HasColumnName("antenna");
        builder.HasIndex(x => x.AntennaId);
        builder.HasOne<Antenna>()
            .WithMany()
            .HasForeignKey(x => x.AntennaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(x => x.FullName);
        builder.Ignore(x => x.IsActive);

        AuditColumns.Apply(builder);
    }
}
