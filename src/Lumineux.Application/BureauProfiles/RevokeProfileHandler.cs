using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.BureauProfiles;

/// <summary>
/// Cas d'usage : révoquer l'attribution d'un profil à un membre (US3, FR-004). Applique le
/// garde-fou triple FR-012a : si le profil porte <c>manage_bureau_profiles</c> et que la révocation
/// laisserait zéro administrateur actif, refus 409 <c>last_administrator</c>.
/// </summary>
public sealed class RevokeProfileHandler
{
    private readonly IBureauProfileRepository _profiles;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;

    public RevokeProfileHandler(
        IBureauProfileRepository profiles,
        ICurrentUser user,
        IAuditLogger audit)
    {
        _profiles = profiles;
        _user = user;
        _audit = audit;
    }

    public async Task HandleAsync(int memberId, int profileId, CancellationToken ct = default)
    {
        if (!_user.HasPermission(Permissions.ManageBureauProfiles))
        {
            _audit.Refused("RevokeProfile", "Droit manque", new { memberId, profileId });
            throw new ForbiddenException("Droit requis pour gérer les profils du bureau.");
        }

        var assignment = await _profiles.GetAssignmentAsync(memberId, profileId, ct)
            ?? throw new NotFoundException("Attribution introuvable.");

        var profile = await _profiles.GetByIdAsync(profileId, ct);
        var profileHasAdminRight = profile is not null
            && profile.Permissions.Any(p => p.Permission == Permissions.ManageBureauProfiles);

        if (profileHasAdminRight)
        {
            var remaining = await _profiles.CountActiveAdministratorsAsync(
                excludeMemberId: memberId, ct: ct);
            if (remaining == 0)
            {
                _audit.Refused("RevokeProfile", "Garde-fou dernier administrateur",
                    new { memberId, profileId });
                throw new ConflictException(
                    "Impossible : cette révocation laisserait le système sans administrateur des profils.",
                    "last_administrator");
            }
        }

        _profiles.RemoveAssignment(assignment);
        await _profiles.SaveChangesAsync(ct);

        _audit.Operation("RevokeProfile", new { memberId, profileId });
    }
}
