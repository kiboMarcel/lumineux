using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Members;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Members;

/// <summary>Cas d'usage : consultation d'une fiche membre (US3, FR-013).</summary>
public sealed class GetMemberHandler
{
    private readonly IMemberRepository _members;
    private readonly IMemberAccountRepository _accounts;
    private readonly ICurrentUser _user;

    public GetMemberHandler(IMemberRepository members, IMemberAccountRepository accounts, ICurrentUser user)
    {
        _members = members;
        _accounts = accounts;
        _user = user;
    }

    public async Task<MemberResponse> HandleAsync(int memberId, CancellationToken ct = default)
    {
        if (!_user.HasPermission(Permissions.ManageMembers))
        {
            throw new ForbiddenException("Droit requis pour gérer les membres.");
        }

        var member = await _members.GetByIdAsync(memberId, ct)
            ?? throw new NotFoundException("Membre introuvable.");

        var account = await _accounts.GetByMemberIdAsync(member.Id, ct);
        return member.ToResponse(account);
    }
}
