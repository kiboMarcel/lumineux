using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Members;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Members;

/// <summary>Cas d'usage : correction d'une fiche membre par le bureau (US3, FR-014).</summary>
public sealed class UpdateMemberHandler
{
    private readonly IMemberRepository _members;
    private readonly IMemberAccountRepository _accounts;
    private readonly IReferenceLookupRepository _lookup;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;
    private readonly IValidator<UpdateMemberRequest> _validator;

    public UpdateMemberHandler(
        IMemberRepository members,
        IMemberAccountRepository accounts,
        IReferenceLookupRepository lookup,
        ICurrentUser user,
        IAuditLogger audit,
        IValidator<UpdateMemberRequest> validator)
    {
        _members = members;
        _accounts = accounts;
        _lookup = lookup;
        _user = user;
        _audit = audit;
        _validator = validator;
    }

    public async Task<MemberResponse> HandleAsync(int memberId, UpdateMemberRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        if (!_user.HasPermission(Permissions.ManageMembers))
        {
            _audit.Refused("UpdateMember", "Droit manage_members manquant", new { memberId });
            throw new ForbiddenException("Droit requis pour gérer les membres.");
        }

        var member = await _members.GetByIdAsync(memberId, ct)
            ?? throw new NotFoundException("Membre introuvable.");

        await MemberReferenceChecks.EnsureExistAsync(
            _lookup, request.AntennaId, request.CivilityId, request.NationalityId,
            request.BirthPlaceId, request.BirthCityId, request.DistrictId, request.IntroducerId, ct);

        // FR-008 : coordonnée déjà utilisée par un AUTRE membre actif.
        if (await _members.IsContactUsedByActiveAsync(request.Email, request.Mobile, excludeMemberId: member.Id, ct))
        {
            _audit.Refused("UpdateMember", "Coordonnée déjà utilisée par un membre actif", new { memberId });
            throw new DuplicateMemberException(
                "Une coordonnée (e-mail ou mobile) est déjà utilisée par un autre membre actif.",
                "contact_in_use",
                Array.Empty<int>());
        }

        member.LastName = request.LastName;
        member.FirstName = request.FirstName;
        member.Gender = request.Gender;
        member.AntennaId = request.AntennaId;
        member.Mobile = request.Mobile;
        member.Email = request.Email;
        member.CivilityId = request.CivilityId;
        member.BirthDate = request.BirthDate;
        member.BirthPlaceId = request.BirthPlaceId;
        member.BirthCityId = request.BirthCityId;
        member.Address = request.Address;
        member.Profession = string.IsNullOrWhiteSpace(request.Profession) ? null : request.Profession.Trim();
        member.DistrictId = request.DistrictId;
        member.NationalityId = request.NationalityId;
        member.IntroducerId = request.IntroducerId;

        await _members.SaveChangesAsync(ct); // audit (updatedt/by) peuplé par l'intercepteur

        _audit.Operation("UpdateMember", new { member.Id, member.Reference });

        var account = await _accounts.GetByMemberIdAsync(member.Id, ct);
        return member.ToResponse(account);
    }
}
