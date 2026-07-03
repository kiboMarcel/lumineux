using Lumineux.Domain.Entities;

namespace Lumineux.Domain.Abstractions;

/// <summary>Résultat paginé d'une recherche de membres.</summary>
public sealed record MemberPage(IReadOnlyList<Member> Items, int Total, int Page, int PageSize);

/// <summary>Port de persistance des membres.</summary>
public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Recherche paginée par nom, prénom ou référence (FR-013).</summary>
    Task<MemberPage> SearchAsync(string? query, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Retourne les membres actifs de même nom+prénom (détection d'homonymes, FR-007).</summary>
    Task<IReadOnlyList<Member>> FindActiveByNameAsync(string lastName, string firstName, CancellationToken ct = default);

    /// <summary>Indique si une coordonnée (e-mail ou mobile) est déjà utilisée par un membre actif (FR-008).</summary>
    /// <param name="excludeMemberId">Membre à exclure (pour la correction d'une fiche existante).</param>
    Task<bool> IsContactUsedByActiveAsync(string? email, string? mobile, int? excludeMemberId, CancellationToken ct = default);

    Task AddAsync(Member member, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
