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

        builder.Property(x => x.Reference).HasColumnName("reference").HasMaxLength(60).IsRequired();
        builder.Property(x => x.EntryDate).HasColumnName("entry_date").IsRequired();
        builder.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Gender).HasColumnName("gender").HasMaxLength(6).IsRequired();
        builder.Property(x => x.CivilityId).HasColumnName("civility");
        builder.Property(x => x.BirthDate).HasColumnName("birth_date");
        builder.Property(x => x.BirthPlaceId).HasColumnName("birth_place");
        builder.Property(x => x.BirthCityId).HasColumnName("birth_city");
        builder.Property(x => x.Mobile).HasColumnName("mobile").HasMaxLength(255);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
        builder.Property(x => x.Address).HasColumnName("address").HasMaxLength(255);
        builder.Property(x => x.DistrictId).HasColumnName("district");
        builder.Property(x => x.NationalityId).HasColumnName("nationality");
        builder.Property(x => x.IntroducerId).HasColumnName("introducer");
        builder.Property(x => x.AntennaId).HasColumnName("antenna");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();

        builder.Ignore(x => x.FullName);
        builder.Ignore(x => x.IsActive);

        // Clés étrangères (FR-005). Aucune navigation exposée.
        builder.HasOne<Antenna>().WithMany().HasForeignKey(x => x.AntennaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Civility>().WithMany().HasForeignKey(x => x.CivilityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Country>().WithMany().HasForeignKey(x => x.NationalityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<City>().WithMany().HasForeignKey(x => x.BirthPlaceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<City>().WithMany().HasForeignKey(x => x.BirthCityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<District>().WithMany().HasForeignKey(x => x.DistrictId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Member>().WithMany().HasForeignKey(x => x.IntroducerId).OnDelete(DeleteBehavior.Restrict);

        // Référence unique (= identifiant de connexion).
        builder.HasIndex(x => x.Reference).IsUnique();

        // Unicité filtrée des contacts d'un membre actif (FR-008) — filtre portable SQL Server / SQLite.
        builder.HasIndex(x => x.Email).IsUnique().HasFilter("email IS NOT NULL AND status = 'Active'");
        builder.HasIndex(x => x.Mobile).IsUnique().HasFilter("mobile IS NOT NULL AND status = 'Active'");

        // Index de recherche (FR-013).
        builder.HasIndex(x => x.LastName);
        builder.HasIndex(x => x.FirstName);

        AuditColumns.Apply(builder);
    }
}
