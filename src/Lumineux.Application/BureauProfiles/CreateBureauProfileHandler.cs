using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.BureauProfiles;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;

namespace Lumineux.Application.BureauProfiles;

/// <summary>Cas d'usage : créer un profil du bureau (US1, FR-001/FR-008/FR-015).</summary>
public sealed class CreateBureauProfileHandler
{
    private readonly IBureauProfileRepository _profiles;
    private readonly IPermissionCatalog _catalog;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;
    private readonly IValidator<BureauProfileWriteRequest> _validator;

    public CreateBureauProfileHandler(
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

    public async Task<BureauProfileDetailResponse> HandleAsync(BureauProfileWriteRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        if (!_user.HasPermission(Permissions.ManageBureauProfiles))
        {
            _audit.Refused("CreateBureauProfile", "Droit manage_bureau_profiles manquant");
            throw new ForbiddenException("Droit requis pour gérer les profils du bureau.");
        }

        var normalized = request.Name.Trim().ToLowerInvariant();
        var existing = await _profiles.GetByNameNormalizedAsync(normalized, ct);
        if (existing is not null)
        {
            _audit.Refused("CreateBureauProfile", "Nom déjà utilisé", new { request.Name });
            throw new ConflictException("Un profil portant ce nom existe déjà.", "duplicate_name");
        }

        BureauProfile profile;
        try
        {
            profile = BureauProfile.Create(request.Name, request.Description, request.Permissions ?? Array.Empty<string>(), _catalog);
        }
        catch (DomainException)
        {
            throw;
        }

        await _profiles.AddAsync(profile, ct);
        await _profiles.SaveChangesAsync(ct);

        _audit.Operation("CreateBureauProfile", new { profile.Id, profile.Name });
        return BureauProfileMapper.ToDetail(profile, memberCount: 0, members: Array.Empty<MemberRefResponse>());
    }
}
