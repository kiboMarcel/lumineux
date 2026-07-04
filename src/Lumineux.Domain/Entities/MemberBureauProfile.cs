namespace Lumineux.Domain.Entities;

/// <summary>
/// Attribution d'un profil du bureau à un membre (feature 004). Unicité `(member, bureauProfile)`.
/// L'idempotence est garantie côté cas d'usage (« insert if not exists »).
/// </summary>
public class MemberBureauProfile : AbstractEntity
{
    public int MemberId { get; set; }

    /// <summary>Navigation (facultative) permettant à EF de résoudre la FK avant sauvegarde.</summary>
    public Member? Member { get; set; }

    public int BureauProfileId { get; set; }

    /// <summary>Navigation (facultative) permettant à EF de résoudre la FK avant sauvegarde.</summary>
    public BureauProfile? BureauProfile { get; set; }
}
