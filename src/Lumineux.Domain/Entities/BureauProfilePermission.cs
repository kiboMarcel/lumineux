namespace Lumineux.Domain.Entities;

/// <summary>Association profil ↔ droit (feature 004). Unicité `(bureauProfile, permission)`.</summary>
public class BureauProfilePermission : AbstractEntity
{
    public int BureauProfileId { get; set; }

    public string Permission { get; set; } = default!;
}
