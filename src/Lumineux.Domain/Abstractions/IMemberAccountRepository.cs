using Lumineux.Domain.Entities;

namespace Lumineux.Domain.Abstractions;

/// <summary>Port de persistance des comptes de connexion.</summary>
public interface IMemberAccountRepository
{
    Task AddAsync(MemberAccount account, CancellationToken ct = default);

    Task<MemberAccount?> GetByMemberIdAsync(int memberId, CancellationToken ct = default);

    /// <summary>Charge un compte suivi pour modification (ex. changement de mot de passe, feature 003).</summary>
    Task<MemberAccount?> GetByMemberIdForUpdateAsync(int memberId, CancellationToken ct = default);

    /// <summary>Charge un compte par identifiant de connexion (avec le membre), pour la connexion (feature 003).</summary>
    Task<MemberAccount?> GetByLoginIdAsync(string loginId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
