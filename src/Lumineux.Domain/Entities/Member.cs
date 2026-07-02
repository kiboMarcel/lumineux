namespace Lumineux.Domain.Entities;

/// <summary>
/// Membre de la communauté. Projection minimale réutilisée par la fonctionnalité de
/// présence ; enrichie de <see cref="AntennaId"/> (antenne d'origine — décision FR-011).
/// Le modèle complet du membre relève de la gestion des membres.
/// </summary>
public class Member : AbstractEntity
{
    public string LastName { get; set; } = default!;

    public string FirstName { get; set; } = default!;

    public string Status { get; set; } = "Active";

    /// <summary>Antenne d'origine du membre (nullable). Instantané copié dans les présences.</summary>
    public int? AntennaId { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public bool IsActive => string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase);
}
