using Lumineux.Domain.Abstractions;

namespace Lumineux.Domain.Entities;

/// <summary>
/// Antenne : lieu où se tiennent les réunions. Projection minimale réutilisée par la
/// fonctionnalité de présence ; la gestion (CRUD) est portée par la feature 016. Les invariants et
/// transitions d'état (création, correction, activation/désactivation) sont portés par le domaine.
/// Les setters restent publics pour préserver les usages existants (features 001/010, seeds/tests).
/// </summary>
public class Antenna : AbstractEntity
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";

    public string Code { get; set; } = default!;

    public string Label { get; set; } = default!;

    public int District { get; set; }

    public string Status { get; set; } = Active;

    public bool IsActive => string.Equals(Status, Active, StringComparison.OrdinalIgnoreCase);

    /// <summary>Crée une antenne active avec les informations obligatoires (FR-001).</summary>
    public static Antenna Create(string code, string label, int districtId)
    {
        var normalizedCode = (code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            throw new DomainException("Le code de l'antenne est requis.");
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainException("Le libellé de l'antenne est requis.");
        }

        if (districtId <= 0)
        {
            throw new DomainException("Le district de rattachement est requis.");
        }

        return new Antenna
        {
            Code = normalizedCode,
            Label = label.Trim(),
            District = districtId,
            Status = Active,
        };
    }

    /// <summary>Corrige le libellé et le district. Le <b>code reste inchangé</b> (FR-004).</summary>
    public void UpdateDetails(string label, int districtId)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainException("Le libellé de l'antenne est requis.");
        }

        if (districtId <= 0)
        {
            throw new DomainException("Le district de rattachement est requis.");
        }

        Label = label.Trim();
        District = districtId;
    }

    /// <summary>Désactive l'antenne (statut logique). Idempotent (FR-005/009).</summary>
    public void Deactivate() => Status = Inactive;

    /// <summary>Réactive l'antenne. Idempotent (FR-005/009).</summary>
    public void Activate() => Status = Active;
}
