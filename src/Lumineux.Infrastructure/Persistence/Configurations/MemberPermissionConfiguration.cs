using Lumineux.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumineux.Infrastructure.Persistence.Configurations;

public sealed class MemberPermissionConfiguration : IEntityTypeConfiguration<MemberPermission>
{
    public void Configure(EntityTypeBuilder<MemberPermission> builder)
    {
        builder.ToTable("member_permissions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MemberId).HasColumnName("member").IsRequired();
        builder.Property(x => x.Permission).HasColumnName("permission").HasMaxLength(60).IsRequired();

        builder.HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.MemberId, x.Permission }).IsUnique();

        AuditColumns.Apply(builder);
    }
}
