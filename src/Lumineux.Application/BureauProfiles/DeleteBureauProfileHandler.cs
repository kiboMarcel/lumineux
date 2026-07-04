using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.BureauProfiles;

/// <summary>
/// Cas d'usage : supprimer un profil (US1, FR-003 + garde-fou FR-012c). Ordre d'évaluation :
/// (1) FR-003 <c>profile_in_use</c> si des attributions existent ;
/// (2) garde-fou <c>last_administrator</c> si le profil porte l'admin et qu'il n'en reste plus.
/// </summary>
public sealed class DeleteBureauProfileHandler
{
    private readonly IBureauProfileRepository _profiles;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;

    public DeleteBureauProfileHandler(
        IBureauProfileRepository profiles,
        ICurrentUser user,
        IAuditLogger audit)
    {
        _profiles = profiles;
        _user = user;
        _audit = audit;
    }

    public async Task HandleAsync(int profileId, CancellationToken ct = default)
    {
        if (!_user.HasPermission(Permissions.ManageBureauProfiles))
        {
            _audit.Refused("DeleteBureauProfile", "Droit manque", new { profileId });
            throw new ForbiddenException("Droit requis pour gérer les profils du bureau.");
        }

        var profile = await _profiles.GetByIdAsync(profileId, ct)
            ?? throw new NotFoundException("Profil introuvable.");

        var assignments = await _profiles.CountAssignmentsAsync(profileId, ct);
        if (assignments > 0)
        {
            _audit.Refused("DeleteBureauProfile", "Profil encore attribué", new { profileId });
            throw new ConflictException(
                "Ce profil est attribué à au moins un membre. Révoquez les attributions avant la suppression.",
                "profile_in_use");
        }

        var isAdminProfile = profile.Permissions.Any(p => p.Permission == Permissions.ManageBureauProfiles);
        if (isAdminProfile)
        {
            var remaining = await _profiles.CountActiveAdministratorsAsync(excludeProfileId: profile.Id, ct: ct);
            if (remaining == 0)
            {
                _audit.Refused("DeleteBureauProfile", "Garde-fou dernier administrateur", new { profileId });
                throw new ConflictException(
                    "Impossible : cette suppression laisserait le système sans administrateur des profils.",
                    "last_administrator");
            }
        }

        _profiles.Remove(profile);
        await _profiles.SaveChangesAsync(ct);

        _audit.Operation("DeleteBureauProfile", new { profileId });
    }
}
