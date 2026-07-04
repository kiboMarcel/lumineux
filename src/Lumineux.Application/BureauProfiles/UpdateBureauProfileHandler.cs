using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.BureauProfiles;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.BureauProfiles;

/// <summary>
/// Cas d'usage : modifier un profil (US1, FR-002). Applique le garde-fou triple (FR-012b) : si le
/// retrait de <c>manage_bureau_profiles</c> laisserait zéro administrateur actif, refus 409.
/// </summary>
public sealed class UpdateBureauProfileHandler
{
    private readonly IBureauProfileRepository _profiles;
    private readonly IPermissionCatalog _catalog;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;
    private readonly IValidator<BureauProfileWriteRequest> _validator;

    public UpdateBureauProfileHandler(
        IBureauProfileRepository profiles,
        IPermissionCatalog catalog,
        ICurrentUser user,
        IAuditLogger audit,
        IValidator<BureauProfileWriteRequest> validator)
    {
        _profiles = profiles;
        _catalog = catalog;
        _user = user;
        _audit = audit;
        _validator = validator;
    }

    public async Task<BureauProfileDetailResponse> HandleAsync(int profileId, BureauProfileWriteRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        if (!_user.HasPermission(Permissions.ManageBureauProfiles))
        {
            _audit.Refused("UpdateBureauProfile", "Droit manque", new { profileId });
            throw new ForbiddenException("Droit requis pour gérer les profils du bureau.");
        }

        var profile = await _profiles.GetByIdAsync(profileId, ct)
            ?? throw new NotFoundException("Profil introuvable.");

        var newNormalized = request.Name.Trim().ToLowerInvariant();
        if (newNormalized != profile.NameNormalized)
        {
            var conflict = await _profiles.GetByNameNormalizedAsync(newNormalized, ct);
            if (conflict is not null && conflict.Id != profile.Id)
            {
                _audit.Refused("UpdateBureauProfile", "Nom déjà utilisé", new { profileId });
                throw new ConflictException("Un profil portant ce nom existe déjà.", "duplicate_name");
            }
        }

        // Garde-fou (FR-012b) : si le profil porte actuellement `manage_bureau_profiles` et que le
        // nouveau n'en contient plus, on doit s'assurer qu'il reste un admin ailleurs.
        var currentlyAdmin = profile.Permissions.Any(p => p.Permission == Permissions.ManageBureauProfiles);
        var willBeAdmin = (request.Permissions ?? Array.Empty<string>()).Contains(Permissions.ManageBureauProfiles);
        if (currentlyAdmin && !willBeAdmin)
        {
            var remaining = await _profiles.CountActiveAdministratorsAsync(excludeProfileId: profile.Id, ct: ct);
            if (remaining == 0)
            {
                _audit.Refused("UpdateBureauProfile", "Garde-fou dernier administrateur", new { profileId });
                throw new ConflictException(
                    "Impossible : cette modification laisserait le système sans administrateur des profils.",
                    "last_administrator");
            }
        }

        profile.Rename(request.Name);
        profile.UpdateDescription(request.Description);
        profile.SetPermissions(request.Permissions ?? Array.Empty<string>(), _catalog);

        await _profiles.SaveChangesAsync(ct);

        var memberCount = await _profiles.CountAssignmentsAsync(profile.Id, ct);

        _audit.Operation("UpdateBureauProfile", new { profile.Id, profile.Name });
        return BureauProfileMapper.ToDetail(profile, memberCount, Array.Empty<MemberRefResponse>());
    }
}
