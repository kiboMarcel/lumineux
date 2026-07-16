using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Members;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;

namespace Lumineux.Application.Members;

/// <summary>
/// Cas d'usage : création d'un membre + provisionnement de son compte (US1, FR-001..012).
/// Atomique (membre + compte en une sauvegarde) ; identifiants transmis par e-mail ou repli bureau.
/// </summary>
public sealed class CreateMemberHandler
{
    private readonly IMemberRepository _members;
    private readonly IMemberAccountRepository _accounts;
    private readonly IReferenceLookupRepository _lookup;
    private readonly IMemberReferenceGenerator _referenceGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;
    private readonly IValidator<CreateMemberRequest> _validator;

    public CreateMemberHandler(
        IMemberRepository members,
        IMemberAccountRepository accounts,
        IReferenceLookupRepository lookup,
        IMemberReferenceGenerator referenceGenerator,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IClock clock,
        ICurrentUser user,
        IAuditLogger audit,
        IValidator<CreateMemberRequest> validator)
    {
        _members = members;
        _accounts = accounts;
        _lookup = lookup;
        _referenceGenerator = referenceGenerator;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _clock = clock;
        _user = user;
        _audit = audit;
        _validator = validator;
    }

    public async Task<MemberCreatedResponse> HandleAsync(CreateMemberRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        if (!_user.HasPermission(Permissions.ManageMembers))
        {
            _audit.Refused("CreateMember", "Droit manage_members manquant");
            throw new ForbiddenException("Droit requis pour gérer les membres.");
        }

        await MemberReferenceChecks.EnsureExistAsync(
            _lookup, request.AntennaId, request.CivilityId, request.NationalityId,
            request.BirthPlaceId, request.BirthCityId, request.DistrictId, request.IntroducerId, ct);

        // FR-008 : refus si une coordonnée est déjà utilisée par un membre actif (non contournable).
        if (await _members.IsContactUsedByActiveAsync(request.Email, request.Mobile, excludeMemberId: null, ct))
        {
            _audit.Refused("CreateMember", "Coordonnée déjà utilisée par un membre actif");
            throw new DuplicateMemberException(
                "Une coordonnée (e-mail ou mobile) est déjà utilisée par un membre actif.",
                "contact_in_use",
                Array.Empty<int>());
        }

        // FR-007 : homonymes (nom + prénom) — avertir + confirmation requise.
        if (!request.ConfirmDuplicate)
        {
            var homonyms = await _members.FindActiveByNameAsync(request.LastName, request.FirstName, ct);
            if (homonyms.Count > 0)
            {
                _audit.Refused("CreateMember", "Homonyme non confirmé", new { request.LastName, request.FirstName });
                throw new DuplicateMemberException(
                    "Un membre de mêmes nom et prénom existe déjà. Confirmez pour créer un homonyme distinct.",
                    "duplicate_name",
                    homonyms.Select(h => h.Id).ToList());
            }
        }

        var now = _clock.UtcNow;
        var reference = await _referenceGenerator.NextAsync(now, ct);

        var member = Member.Create(reference, now, request.LastName, request.FirstName, request.Gender, request.AntennaId);
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

        var temporaryPassword = _passwordHasher.GenerateTemporaryPassword();
        var account = MemberAccount.Provision(member, _passwordHasher.Hash(temporaryPassword));

        await _members.AddAsync(member, ct);
        await _accounts.AddAsync(account, ct);
        await _members.SaveChangesAsync(ct); // membre + compte : une seule transaction (FR-006)

        // Envoi non bloquant : la création est déjà persistée. Repli bureau si pas d'e-mail ou échec.
        var outcome = await _emailSender.SendInvitationAsync(member.Email, member.Reference, temporaryPassword, ct);
        var delivery = outcome == EmailSendOutcome.Sent
            ? CredentialsDelivery.EmailSent
            : CredentialsDelivery.BureauHandout;
        var passwordForBureau = delivery == CredentialsDelivery.BureauHandout ? temporaryPassword : null;

        _audit.Operation("CreateMember", new { member.Id, member.Reference, delivery });

        return new MemberCreatedResponse(member.ToResponse(account), member.Reference, delivery, passwordForBureau);
    }
}
