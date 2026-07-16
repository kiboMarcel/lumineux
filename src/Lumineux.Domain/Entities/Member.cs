using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Enums;

namespace Lumineux.Domain.Entities;

/// <summary>
/// Membre de la communauté. Enrichi (feature 002) avec l'identité complète et les rattachements.
/// Les setters restent publics pour préserver les usages existants (feature 001) ; la fabrique
/// <see cref="Create"/> garantit les invariants obligatoires à la création (FR-003/004).
/// </summary>
public class Member : AbstractEntity
{
    /// <summary>Référence unique = identifiant de connexion (FR-004, Q1).</summary>
    public string Reference { get; set; } = default!;

    public DateTime EntryDate { get; set; }

    public string LastName { get; set; } = default!;

    public string FirstName { get; set; } = default!;

    public string Gender { get; set; } = default!;

    public int? CivilityId { get; set; }

    public DateTime? BirthDate { get; set; }

    public int? BirthPlaceId { get; set; }

    public int? BirthCityId { get; set; }

    public string? Mobile { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    /// <summary>Profession du membre (feature 030), texte libre optionnel borné. Aucune unicité.</summary>
    public string? Profession { get; set; }

    public int? DistrictId { get; set; }

    public int? NationalityId { get; set; }

    /// <summary>Membre introducteur (auto-référence), nullable.</summary>
    public int? IntroducerId { get; set; }

    /// <summary>Antenne d'origine (feature 001), nullable.</summary>
    public int? AntennaId { get; set; }

    public string Status { get; set; } = MemberStatuses.Active;

    public string FullName => $"{FirstName} {LastName}".Trim();

    public bool IsActive => string.Equals(Status, MemberStatuses.Active, StringComparison.OrdinalIgnoreCase);

    /// <summary>Crée un nouveau membre actif avec les informations obligatoires (FR-003/004).</summary>
    public static Member Create(
        string reference,
        DateTime entryDateUtc,
        string lastName,
        string firstName,
        string gender,
        int antennaId)
    {
        if (antennaId <= 0)
        {
            throw new DomainException("L'antenne d'origine est requise.");
        }
        return Create(reference, entryDateUtc, lastName, firstName, gender, (int?)antennaId);
    }

    /// <summary>
    /// Surcharge acceptant une antenne d'origine <b>optionnelle</b> (feature 005 — installation du
    /// premier administrateur : le super-admin n'est pas nécessairement rattaché à une antenne au
    /// moment de son provisionnement). Les autres invariants (référence, nom, prénom, genre) sont
    /// vérifiés à l'identique.
    /// </summary>
    public static Member Create(
        string reference,
        DateTime entryDateUtc,
        string lastName,
        string firstName,
        string gender,
        int? antennaId)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new DomainException("La référence du membre est requise.");
        }

        if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("Le nom et le prénom sont requis.");
        }

        if (!Genders.IsValid(gender))
        {
            throw new DomainException("Le sexe doit être 'M' ou 'F'.");
        }

        if (antennaId is int a && a <= 0)
        {
            throw new DomainException("L'antenne d'origine est requise.");
        }

        return new Member
        {
            Reference = reference,
            EntryDate = entryDateUtc,
            LastName = lastName,
            FirstName = firstName,
            Gender = gender,
            AntennaId = antennaId,
            Status = MemberStatuses.Active,
        };
    }
}
