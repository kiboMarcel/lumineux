using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.BureauProfiles;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;

namespace Lumineux.Application.BureauProfiles;

/// <summary>
/// Cas d'usage : attribuer un profil du bureau à un membre (US2, FR-004/FR-005/FR-014).
/// Idempotent : une nouvelle attribution du même couple `(membre, profil)` NE crée PAS de doublon.
/// </summary>
public sealed class AssignProfileHandler
{
    private readonly IBureauProfileRepository _profiles;
    private readonly IMemberRepository _members;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;
    private readonly IValidator<AssignProfileRequest> _validator;

    public AssignProfileHandler(
        IBureauProfileRepository profiles,
        IMemberRepository members,
        ICurrentUser user,
        IAuditLogger audit,
        IValidator<AssignProfileRequest> validator)
    {
        _profiles = profiles;
        _members = members;
        _user = user;
        _audit = audit;
        _validator = validator;
    }

    public async Task HandleAsync(int memberId, AssignProfileRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        if (!_user.HasPermission(Permissions.ManageBureauProfiles))
        {
            _audit.Refused("AssignProfile", "Droit manque", new { memberId, request.ProfileId });
            throw new ForbiddenException("Droit requis pour gérer les profils du bureau.");
        }

        var member = await _members.GetByIdAsync(memberId, ct)
            ?? throw new NotFoundException("Membre introuvable.");
        var profile = await _profiles.GetByIdAsync(request.ProfileId, ct)
            ?? throw new NotFoundException("Profil introuvable.");

        if (!member.IsActive)
        {
            _audit.Refused("AssignProfile", "Membre inactif", new { memberId, request.ProfileId });
            throw new ConflictException(
                "L'attribution n'a de sens que pour un membre actif.",
                "member_inactive");
        }

        var existing = await _profiles.GetAssignmentAsync(memberId, profile.Id, ct);
        if (existing is not null)
        {
            _audit.Operation("AssignProfile", new { memberId, profile.Id, idempotent = true });
            return;
        }

        await _profiles.AddAssignmentAsync(
            new MemberBureauProfile { MemberId = memberId, BureauProfileId = profile.Id }, ct);
        await _profiles.SaveChangesAsync(ct);

        _audit.Operation("AssignProfile", new { memberId, profile.Id });
    }
}
