using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumineux.Infrastructure.Persistence.Configurations;

public sealed class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> builder)
    {
        builder.ToTable("attendance_sessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AntennaId).HasColumnName("antenna").IsRequired();
        builder.Property(x => x.MeetingDate).HasColumnName("meeting_date").IsRequired();
        builder.Property(x => x.StartTime).HasColumnName("start_time").IsRequired();
        builder.Property(x => x.EndTime).HasColumnName("end_time");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.SessionType).HasColumnName("session_type").HasConversion<string>().HasMaxLength(20).IsRequired().HasDefaultValue(SessionType.AntennaMeeting);
        builder.Property(x => x.OpenedByMemberId).HasColumnName("opened_by").IsRequired();
        builder.Property(x => x.ClosedByMemberId).HasColumnName("closed_by");
        builder.Property(x => x.CancelledByMemberId).HasColumnName("cancelled_by");
        builder.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
        builder.Property(x => x.QrSecret).HasColumnName("qr_secret").HasMaxLength(512).IsRequired();
        builder.Property(x => x.QrStepSeconds).HasColumnName("qr_step_seconds").IsRequired();

        builder.HasOne<Antenna>()
            .WithMany()
            .HasForeignKey(x => x.AntennaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Retrouve rapidement la session ouverte d'une antenne (FR-003).
        builder.HasIndex(x => new { x.AntennaId, x.Status });

        AuditColumns.Apply(builder);
    }
}
