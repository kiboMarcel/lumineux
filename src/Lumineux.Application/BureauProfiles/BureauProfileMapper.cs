using Lumineux.Application.Contracts.BureauProfiles;
using Lumineux.Domain.Entities;

namespace Lumineux.Application.BureauProfiles;

internal static class BureauProfileMapper
{
    public static BureauProfileSummaryResponse ToSummary(BureauProfile profile, int memberCount) =>
        new(profile.Id, profile.Name, profile.Description,
            profile.Permissions.Select(p => p.Permission).OrderBy(p => p, StringComparer.Ordinal).ToList(),
            memberCount);

    public static BureauProfileDetailResponse ToDetail(BureauProfile profile, int memberCount, IReadOnlyList<MemberRefResponse> members) =>
        new(profile.Id, profile.Name, profile.Description,
            profile.Permissions.Select(p => p.Permission).OrderBy(p => p, StringComparer.Ordinal).ToList(),
            memberCount, members);

    public static MemberRefResponse ToMemberRef(Member member) =>
        new(member.Id, member.Reference, member.FullName, member.Status);
}
