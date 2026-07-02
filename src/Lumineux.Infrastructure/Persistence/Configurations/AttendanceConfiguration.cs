using Lumineux.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumineux.Infrastructure.Persistence.Configurations;

public sealed class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("attendances");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SessionId).HasColumnName("session").IsRequired();
        builder.Property(x => x.MemberId).HasColumnName("member").IsRequired();
        builder.Property(x => x.ArrivalTime).HasColumnName("arrival_time").IsRequired();
        builder.Property(x => x.EndTime).HasColumnName("end_time");
        builder.Property(x => x.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.OriginAntennaId).HasColumnName("origin_antenna");
        builder.Property(x => x.ClientOperationId).HasColumnName("client_operation_id").HasMaxLength(64);

        builder.HasOne<AttendanceSession>()
            .WithMany()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Antenna>()
            .WithMany()
            .HasForeignKey(x => x.OriginAntennaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Anti-doublon : un membre au plus une présence VALIDE par session (FR-010).
        // Filtre exprimé sans quoting d'identifiant pour rester portable SQL Server / SQLite.
        builder.HasIndex(x => new { x.SessionId, x.MemberId })
            .IsUnique()
            .HasFilter("status = 'Valid'");

        // Idempotence des scans hors ligne (FR-023a).
        builder.HasIndex(x => new { x.SessionId, x.ClientOperationId })
            .IsUnique()
            .HasFilter("client_operation_id IS NOT NULL");

        // Consultation en direct / post-clôture (FR-021/022).
        builder.HasIndex(x => new { x.SessionId, x.Status });

        AuditColumns.Apply(builder);
    }
}
