using Lumineux.Domain.Abstractions;

namespace Lumineux.Domain.Entities;

/// <summary>
/// Jeton de réinitialisation de mot de passe à usage unique et durée de vie limitée (feature 006).
/// Seule l'empreinte du jeton est persistée — le jeton en clair n'est jamais stocké côté serveur
/// (FR-009). Le cycle de vie (validité, consommation) est porté par le domaine (Constitution I).
/// </summary>
public class PasswordResetToken : AbstractEntity
{
    /// <summary>Compte de connexion cible du reset.</summary>
    public int AccountId { get; private set; }

    /// <summary>Navigation vers le compte (chargée pour la mise à jour lors du reset).</summary>
    public MemberAccount Account { get; private set; } = default!;

    /// <summary>Empreinte cryptographique du jeton (jamais le clair, FR-009/016).</summary>
    public string TokenHash { get; private set; } = default!;

    /// <summary>Fin de validité (UTC), FR-004.</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Horodatage de consommation (usage unique, FR-007b) ; null = non consommé.</summary>
    public DateTime? ConsumedAt { get; private set; }

    // Requis par EF Core.
    private PasswordResetToken() { }

    /// <summary>
    /// Émet un nouveau jeton pour un compte, expirant après <paramref name="lifetimeMinutes"/>. Le
    /// lien se fait par navigation (la FK <c>AccountId</c> est renseignée par EF à la sauvegarde) —
    /// même idiome que <see cref="MemberAccount.Provision"/>.
    /// </summary>
    public static PasswordResetToken Issue(MemberAccount account, string tokenHash, DateTime nowUtc, int lifetimeMinutes)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException("L'empreinte du jeton est requise.");
        }

        if (lifetimeMinutes <= 0)
        {
            throw new DomainException("La durée de vie du jeton doit être strictement positive.");
        }

        return new PasswordResetToken
        {
            Account = account,
            TokenHash = tokenHash,
            ExpiresAt = nowUtc.AddMinutes(lifetimeMinutes),
            ConsumedAt = null,
        };
    }

    /// <summary>Indique si le jeton est encore utilisable (ni consommé, ni expiré) — FR-004/007b.</summary>
    public bool IsUsable(DateTime nowUtc) => ConsumedAt is null && ExpiresAt > nowUtc;

    /// <summary>Marque le jeton comme consommé (usage unique, FR-007b). Refuse une double consommation.</summary>
    public void Consume(DateTime nowUtc)
    {
        if (ConsumedAt is not null)
        {
            throw new DomainException("Ce jeton de réinitialisation a déjà été consommé.");
        }

        ConsumedAt = nowUtc;
    }
}
