using Lumineux.Domain.Entities;

namespace Lumineux.Domain.Abstractions;

/// <summary>Port de persistance des comptes de connexion.</summary>
public interface IMemberAccountRepository
{
    Task AddAsync(MemberAccount account, CancellationToken ct = default);

    Task<MemberAccount?> GetByMemberIdAsync(int memberId, CancellationToken ct = default);
}
