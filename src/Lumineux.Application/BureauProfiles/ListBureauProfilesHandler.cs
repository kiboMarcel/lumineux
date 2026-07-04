using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.BureauProfiles;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.BureauProfiles;

/// <summary>Lister les profils avec droits + nombre de titulaires (US4, FR-009).</summary>
public sealed class ListBureauProfilesHandler
{
    private readonly IBureauProfileRepository _profiles;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;

    public ListBureauProfilesHandler(
        IBureauProfileRepository profiles,
        ICurrentUser user,
        IAuditLogger audit)
    {
        _profiles = profiles;
        _user = user;
        _audit = audit;
    }

    public async Task<IReadOnlyList<BureauProfileSummaryResponse>> HandleAsync(CancellationToken ct = default)
    {
        if (!ReadAccess.HasReadAccess(_user))
        {
            _audit.Refused("ListBureauProfiles", "Droit manque");
            throw new ForbiddenException("Droit requis pour consulter les profils du bureau.");
        }

        var profiles = await _profiles.ListAllAsync(ct);
        var counts = await _profiles.CountAssignmentsByProfileAsync(ct);

        return profiles.Select(p =>
                BureauProfileMapper.ToSummary(p, counts.TryGetValue(p.Id, out var c) ? c : 0))
            .ToList();
    }
}
