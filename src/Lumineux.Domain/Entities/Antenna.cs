namespace Lumineux.Domain.Entities;

/// <summary>
/// Antenne : lieu où se tiennent les réunions. Projection minimale réutilisée par la
/// fonctionnalité de présence (le modèle complet relève de la gestion des antennes).
/// </summary>
public class Antenna : AbstractEntity
{
    public string Code { get; set; } = default!;

    public string Label { get; set; } = default!;

    public int District { get; set; }

    public string Status { get; set; } = "Active";
}
