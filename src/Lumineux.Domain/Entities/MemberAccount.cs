using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Enums;

namespace Lumineux.Domain.Entities;

/// <summary>
/// Compte de connexion rattaché à un membre (1-1). L'identifiant de connexion est la référence
/// membre. Le mot de passe n'est stocké que sous forme d'empreinte (jamais en clair).
/// </summary>
public class MemberAccount : AbstractEntity
{
    /// <summary>Navigation vers le membre (permet l'insertion atomique membre + compte).</summary>
    public Member Member { get; private set; } = default!;

    public int MemberId { get; private set; }

    /// <summary>Identifiant de connexion (= référence membre).</summary>
    public string LoginId { get; private set; } = default!;

    /// <summary>Empreinte du mot de passe — jamais en clair, jamais exposée.</summary>
    public string PasswordHash { get; private set; } = default!;

    public bool MustChangePassword { get; private set; }

    public AccountActivationState ActivationState { get; private set; }

    // Requis par EF Core.
    private MemberAccount() { }

    /// <summary>
    /// Provisionne un compte pour un nouveau membre (FR-009/010). Le lien se fait par navigation :
    /// la clé étrangère est renseignée par EF lors de la sauvegarde (id du membre généré).
    /// </summary>
    public static MemberAccount Provision(Member member, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (string.IsNullOrWhiteSpace(member.Reference))
        {
            throw new DomainException("La référence du membre est requise pour provisionner le compte.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("L'empreinte du mot de passe est requise.");
        }

        return new MemberAccount
        {
            Member = member,
            LoginId = member.Reference,
            PasswordHash = passwordHash,
            MustChangePassword = true,
            ActivationState = AccountActivationState.PendingActivation,
        };
    }
}
