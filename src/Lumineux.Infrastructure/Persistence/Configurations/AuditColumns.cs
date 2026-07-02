using Lumineux.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumineux.Infrastructure.Persistence.Configurations;

/// <summary>Applique le mapping commun des champs d'audit hérités d'AbstractEntity.</summary>
internal static class AuditColumns
{
    public static void Apply<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : AbstractEntity
    {
        builder.Property(x => x.CreatedAt).HasColumnName("createdt").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("createdby").HasMaxLength(255);
        builder.Property(x => x.UpdatedAt).HasColumnName("updatedt");
        builder.Property(x => x.UpdatedBy).HasColumnName("updatedby").HasMaxLength(255);
    }
}
