using Lumineux.Domain.Entities;

namespace Lumineux.Domain.Abstractions;

/// <summary>Accès aux profils du bureau et à leurs attributions (feature 004).</summary>
public interface IBureauProfileRepository
{
    Task AddAsync(BureauProfile profile, CancellationToken ct = default);

    Task<BureauProfile?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<BureauProfile?> GetByNameNormalizedAsync(string nameNormalized, CancellationToken ct = default);

    Task<IReadOnlyList<BureauProfile>> ListAllAsync(CancellationToken ct = default);

    void Remove(BureauProfile profile);

    /// <summary>Nombre d'attributions vivantes du profil (utilisé pour bloquer la suppression, FR-003).</summary>
    Task<int> CountAssignmentsAsync(int profileId, CancellationToken ct = default);

    /// <summary>
    /// Compte les membres actifs disposant du droit <c>manage_bureau_profiles</c> après avoir
    /// éventuellement exclu un profil (ex. simulation d'un retrait de droit / d'une suppression)
    /// et/ou une attribution (ex. simulation d'une révocation). Sert au garde-fou triple (FR-012).
    /// </summary>
    Task<int> CountActiveAdministratorsAsync(
        int? excludeProfileId = null,
        int? excludeMemberId = null,
        CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, int>> CountAssignmentsByProfileAsync(CancellationToken ct = default);

    Task<MemberBureauProfile?> GetAssignmentAsync(int memberId, int profileId, CancellationToken ct = default);

    Task<IReadOnlyList<MemberBureauProfile>> GetAssignmentsByProfileAsync(int profileId, CancellationToken ct = default);

    Task<IReadOnlyList<BureauProfile>> GetProfilesForMemberAsync(int memberId, CancellationToken ct = default);

    Task AddAssignmentAsync(MemberBureauProfile assignment, CancellationToken ct = default);

    void RemoveAssignment(MemberBureauProfile assignment);

    Task SaveChangesAsync(CancellationToken ct = default);
}
