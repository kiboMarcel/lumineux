using Lumineux.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumineux.Infrastructure.Persistence.Configurations;

public sealed class CivilityConfiguration : IEntityTypeConfiguration<Civility>
{
    public void Configure(EntityTypeBuilder<Civility> b)
    {
        b.ToTable("civilities");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(60).IsRequired();
        b.Property(x => x.Label).HasColumnName("label").HasMaxLength(100).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        AuditColumns.Apply(b);
    }
}

public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> b)
    {
        b.ToTable("countries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(10).IsRequired();
        b.Property(x => x.LabelCountry).HasColumnName("label_country").HasMaxLength(200).IsRequired();
        b.Property(x => x.LabelNationality).HasColumnName("label_nationality").HasMaxLength(210).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        AuditColumns.Apply(b);
    }
}

public sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> b)
    {
        b.ToTable("cities");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(10).IsRequired();
        b.Property(x => x.Label).HasColumnName("label").HasMaxLength(150).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        AuditColumns.Apply(b);
    }
}

public sealed class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> b)
    {
        b.ToTable("districts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(10).IsRequired();
        b.Property(x => x.Label).HasColumnName("label").HasMaxLength(150).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        AuditColumns.Apply(b);
    }
}
