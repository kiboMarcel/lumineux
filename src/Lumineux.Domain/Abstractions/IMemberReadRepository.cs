using Lumineux.Domain.Entities;

namespace Lumineux.Domain.Abstractions;

/// <summary>Accès en lecture aux membres (réutilisés, non gérés par cette fonctionnalité).</summary>
public interface IMemberReadRepository
{
    Task<Member?> GetByIdAsync(int memberId, CancellationToken ct = default);

    /// <summary>Charge plusieurs membres par id (pour enrichir une liste de présences).</summary>
    Task<IReadOnlyDictionary<int, Member>> GetByIdsAsync(IReadOnlyCollection<int> memberIds, CancellationToken ct = default);
}
