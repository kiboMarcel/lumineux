using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.BureauProfiles;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.BureauProfiles;

/// <summary>Détail d'un profil : droits + titulaires (US4, FR-009).</summary>
public sealed class GetBureauProfileHandler
{
    private readonly IBureauProfileRepository _profiles;
    private readonly IMemberRepository _members;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;

    public GetBureauProfileHandler(
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

    public async Task<BureauProfileDetailResponse> HandleAsync(int profileId, CancellationToken ct = default)
    {
        if (!ReadAccess.HasReadAccess(_user))
        {
            _audit.Refused("GetBureauProfile", "Droit manque", new { profileId });
            throw new ForbiddenException("Droit requis pour consulter les profils du bureau.");
        }

        var profile = await _profiles.GetByIdAsync(profileId, ct)
            ?? throw new NotFoundException("Profil introuvable.");

        var assignments = await _profiles.GetAssignmentsByProfileAsync(profileId, ct);
        var memberRefs = new List<MemberRefResponse>();
        foreach (var a in assignments)
        {
            var m = await _members.GetByIdAsync(a.MemberId, ct);
            if (m is not null)
            {
                memberRefs.Add(BureauProfileMapper.ToMemberRef(m));
            }
        }

        return BureauProfileMapper.ToDetail(profile, memberRefs.Count, memberRefs);
    }
}
