using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Members;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Members;

/// <summary>Cas d'usage : recherche paginée de membres (US3, FR-013).</summary>
public sealed class SearchMembersHandler
{
    private readonly IMemberRepository _members;
    private readonly ICurrentUser _user;

    public SearchMembersHandler(IMemberRepository members, ICurrentUser user)
    {
        _members = members;
        _user = user;
    }

    public async Task<MemberListResponse> HandleAsync(string? query, int page, int pageSize, CancellationToken ct = default)
    {
        if (!_user.HasPermission(Permissions.ManageMembers))
        {
            throw new ForbiddenException("Droit requis pour gérer les membres.");
        }

        var result = await _members.SearchAsync(query, page, pageSize, ct);
        var items = result.Items.Select(m => m.ToListItem()).ToList();
        return new MemberListResponse(result.Page, result.PageSize, result.Total, items);
    }
}
