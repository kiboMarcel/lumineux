using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.BureauProfiles;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.BureauProfiles;

/// <summary>Profils d'un membre + droits effectifs (US4, FR-006/FR-009).</summary>
public sealed class GetMemberProfilesHandler
{
    private readonly IBureauProfileRepository _profiles;
    private readonly IMemberRepository _members;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;

    public GetMemberProfilesHandler(
        IBureauProfileRepository profiles,
        IMemberRepository members,
        ICurrentUser user,
        IAuditLogger audit)
    {
        _profiles = profiles;
        _members = members;
        _user = user;
        _audit = audit;
    }

    public async Task<MemberProfilesResponse> HandleAsync(int memberId, CancellationToken ct = default)
    {
        if (!ReadAccess.HasReadAccess(_user))
        {
            _audit.Refused("GetMemberProfiles", "Droit manque", new { memberId });
            throw new ForbiddenException("Droit requis pour consulter les profils du bureau.");
        }

        var member = await _members.GetByIdAsync(memberId, ct)
            ?? throw new NotFoundException("Membre introuvable.");

        var profiles = await _profiles.GetProfilesForMemberAsync(memberId, ct);
        var summaries = profiles
            .Select(p => BureauProfileMapper.ToSummary(p, memberCount: 0))
            .ToList();

        var effective = profiles
            .SelectMany(p => p.Permissions.Select(bp => bp.Permission))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        return new MemberProfilesResponse(BureauProfileMapper.ToMemberRef(member), summaries, effective);
    }
}
