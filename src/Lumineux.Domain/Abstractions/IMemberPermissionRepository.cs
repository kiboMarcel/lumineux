using Lumineux.Domain.Entities;

namespace Lumineux.Domain.Abstractions;

/// <summary>Accès aux droits d'un membre (lus à la connexion pour peupler le jeton, feature 003).</summary>
public interface IMemberPermissionRepository
{
    Task<IReadOnlyList<string>> GetPermissionsAsync(int memberId, CancellationToken ct = default);

    Task<bool> HasPermissionAsync(int memberId, string permission, CancellationToken ct = default);

    Task AddAsync(MemberPermission permission, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
