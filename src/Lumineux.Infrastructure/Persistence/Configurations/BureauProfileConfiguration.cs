using Lumineux.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumineux.Infrastructure.Persistence.Configurations;

public sealed class BureauProfileConfiguration : IEntityTypeConfiguration<BureauProfile>
{
    public void Configure(EntityTypeBuilder<BureauProfile> builder)
    {
        builder.ToTable("bureau_profiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(BureauProfile.NameMaxLength).IsRequired();
        builder.Property(x => x.NameNormalized).HasColumnName("name_normalized").HasMaxLength(BureauProfile.NameMaxLength).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(BureauProfile.DescriptionMaxLength);

        builder.HasIndex(x => x.NameNormalized).IsUnique();

        builder.HasMany(x => x.Permissions)
            .WithOne()
            .HasForeignKey(x => x.BureauProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        AuditColumns.Apply(builder);
    }
}

public sealed class BureauProfilePermissionConfiguration : IEntityTypeConfiguration<BureauProfilePermission>
{
    public void Configure(EntityTypeBuilder<BureauProfilePermission> builder)
    {
        builder.ToTable("bureau_profile_permissions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BureauProfileId).HasColumnName("bureau_profile").IsRequired();
        builder.Property(x => x.Permission).HasColumnName("permission").HasMaxLength(60).IsRequired();

        builder.HasIndex(x => new { x.BureauProfileId, x.Permission }).IsUnique();

        AuditColumns.Apply(builder);
    }
}

public sealed class MemberBureauProfileConfiguration : IEntityTypeConfiguration<MemberBureauProfile>
{
    public void Configure(EntityTypeBuilder<MemberBureauProfile> builder)
    {
        builder.ToTable("member_bureau_profiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MemberId).HasColumnName("member").IsRequired();
        builder.Property(x => x.BureauProfileId).HasColumnName("bureau_profile").IsRequired();

        builder.HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<BureauProfile>().WithMany().HasForeignKey(x => x.BureauProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.MemberId, x.BureauProfileId }).IsUnique();

        AuditColumns.Apply(builder);
    }
}
