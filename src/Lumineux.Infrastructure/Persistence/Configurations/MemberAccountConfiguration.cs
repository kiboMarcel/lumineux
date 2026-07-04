using Lumineux.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumineux.Infrastructure.Persistence.Configurations;

public sealed class MemberAccountConfiguration : IEntityTypeConfiguration<MemberAccount>
{
    public void Configure(EntityTypeBuilder<MemberAccount> builder)
    {
        builder.ToTable("member_accounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MemberId).HasColumnName("member").IsRequired();
        builder.Property(x => x.LoginId).HasColumnName("login_id").HasMaxLength(60).IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();
        builder.Property(x => x.MustChangePassword).HasColumnName("must_change_password").IsRequired();
        builder.Property(x => x.ActivationState).HasColumnName("activation_state")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.FailedAttempts).HasColumnName("failed_attempts").IsRequired();
        builder.Property(x => x.LockoutUntil).HasColumnName("lockout_until");
        builder.Property(x => x.LastLoginAt).HasColumnName("last_login_at");

        // 1-1 avec Member (navigation → insertion atomique membre + compte).
        builder.HasIndex(x => x.MemberId).IsUnique();
        builder.HasIndex(x => x.LoginId).IsUnique();
        builder.HasOne(x => x.Member).WithOne().HasForeignKey<MemberAccount>(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);

        AuditColumns.Apply(builder);
    }
}
