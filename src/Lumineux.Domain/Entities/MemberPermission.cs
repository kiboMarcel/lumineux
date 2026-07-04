namespace Lumineux.Domain.Entities;

/// <summary>
/// Droit accordé à un membre (source des claims du jeton, feature 003). L'attribution complète
/// (profils du bureau) relève d'une autre fonctionnalité ; cette fonctionnalité lit ces droits.
/// </summary>
public class MemberPermission : AbstractEntity
{
    public int MemberId { get; set; }

    public string Permission { get; set; } = default!;
}
