using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Members;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Members;

/// <summary>
/// Cas d'usage : recherche membre allégée (feature 015) pour identifier un membre lors de l'ajout
/// manuel d'une présence. Accès élargi (any-of) : <c>manage_attendance</c> OU <c>manage_members</c>.
/// Terme requis (anti-aspiration), résultats plafonnés, projection vers des champs minimaux (aucune
/// donnée personnelle superflue). Lecture seule ; réutilise la recherche existante.
/// </summary>
public sealed class LookupMembersHandler
{
    /// <summary>Plafond de résultats (liste courte, FR-004).</summary>
    private const int MaxResults = 20;

    private readonly IMemberRepository _members;
    private readonly ICurrentUser _user;

    public LookupMembersHandler(IMemberRepository members, ICurrentUser user)
    {
        _members = members;
        _user = user;
    }

    public async Task<IReadOnlyList<MemberLookupResponse>> HandleAsync(string? query, CancellationToken ct = default)
    {
        // Lecture élargie : gestion des présences OU gestion des membres (FR-005).
        if (!_user.HasPermission(Permissions.ManageAttendance) && !_user.HasPermission(Permissions.ManageMembers))
        {
            throw new ForbiddenException("Droit requis pour rechercher un membre.");
        }

        // Terme requis : pas de listing complet du fichier des membres (FR-002).
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new DomainException("Un terme de recherche est requis.");
        }

        var page = await _members.SearchAsync(query.Trim(), page: 1, pageSize: MaxResults, ct);
        return page.Items
            .Select(m => new MemberLookupResponse(m.Id, m.Reference, m.FullName, m.Status))
            .ToList();
    }
}
