using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Auth;
using Lumineux.Application.Contracts.Setup;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;

namespace Lumineux.Application.Setup;

/// <summary>
/// Cas d'usage : installation du premier administrateur sur base vierge (feature 005).
/// Compose Member/MemberAccount/BureauProfile via les ports existants. Refuse dès qu'un admin
/// actif existe (verrou naturel FR-004) ; le refus est prioritaire sur la validation (FR-005).
/// </summary>
public sealed class InstallFirstAdminHandler
{
    public const string AdminProfileName = "Administrateur";
    private const string AdminProfileDescription =
        "Profil système créé lors de l'installation initiale (feature 005).";

    private readonly IBureauProfileRepository _profiles;
    private readonly IMemberRepository _members;
    private readonly IMemberAccountRepository _accounts;
    private readonly IMemberReferenceGenerator _referenceGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPermissionCatalog _catalog;
    private readonly IEffectivePermissionsReader _permissions;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly IValidator<InstallFirstAdminRequest> _validator;

    public InstallFirstAdminHandler(
        IBureauProfileRepository profiles,
        IMemberRepository members,
        IMemberAccountRepository accounts,
        IMemberReferenceGenerator referenceGenerator,
        IPasswordHasher passwordHasher,
        IPermissionCatalog catalog,
        IEffectivePermissionsReader permissions,
        ITokenIssuer tokenIssuer,
        IClock clock,
        IAuditLogger audit,
        IValidator<InstallFirstAdminRequest> validator)
    {
        _profiles = profiles;
        _members = members;
        _accounts = accounts;
        _referenceGenerator = referenceGenerator;
        _passwordHasher = passwordHasher;
        _catalog = catalog;
        _permissions = permissions;
        _tokenIssuer = tokenIssuer;
        _clock = clock;
        _audit = audit;
        _validator = validator;
    }

    public async Task<TokenResponse> HandleAsync(InstallFirstAdminRequest request, CancellationToken ct = default)
    {
        // PRIORITÉ 1 — verrou naturel (FR-004/FR-005) : refus AVANT toute validation du payload
        // pour ne divulguer aucune information sur la structure attendue à un tiers.
        var activeAdmins = await _profiles.CountActiveAdministratorsAsync(ct: ct);
        if (activeAdmins > 0)
        {
            _audit.Refused("Setup.FirstAdmin", "Déjà installé");
            throw new ConflictException(
                "Le système est déjà installé — un administrateur actif existe.",
                "already_installed");
        }

        await _validator.ValidateAndThrowAsync(request, ct);

        // FR-014 : vérification de collision de coordonnée (email/mobile).
        if (!string.IsNullOrWhiteSpace(request.Email) || !string.IsNullOrWhiteSpace(request.Mobile))
        {
            if (await _members.IsContactUsedByActiveAsync(request.Email, request.Mobile, excludeMemberId: null, ct))
            {
                _audit.Refused("Setup.FirstAdmin", "Coordonnée déjà utilisée");
                throw new ConflictException(
                    "Une coordonnée (email ou mobile) est déjà utilisée par un membre actif.",
                    "contact_in_use");
            }
        }

        // Création atomique (une seule SaveChanges).
        var now = _clock.UtcNow;
        var reference = await _referenceGenerator.NextAsync(now, ct);

        // 1) Member : surcharge nullable de la feature 005 (AntennaId = null autorisé).
        var member = Member.Create(reference, now, request.LastName, request.FirstName, request.Gender, antennaId: (int?)null);
        member.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email;
        member.Mobile = string.IsNullOrWhiteSpace(request.Mobile) ? null : request.Mobile;
        await _members.AddAsync(member, ct);

        // 2) MemberAccount actif (mot de passe fourni = final, MustChangePassword levé).
        var hash = _passwordHasher.Hash(request.Password);
        var account = MemberAccount.Provision(member, hash);
        account.ChangePassword(hash);
        account.Activate();
        await _accounts.AddAsync(account, ct);

        // 3) Profil « Administrateur » (créé si absent — FR-008/013 : idempotence, on NE modifie
        //    PAS un profil existant, ni ses droits ni sa description).
        var normalized = AdminProfileName.ToLowerInvariant();
        var profile = await _profiles.GetByNameNormalizedAsync(normalized, ct);
        if (profile is null)
        {
            var allPermissions = _catalog.All().Select(d => d.Code).ToList();
            profile = BureauProfile.Create(AdminProfileName, AdminProfileDescription, allPermissions, _catalog);
            await _profiles.AddAsync(profile, ct);
        }

        // 4) Attribution : membre → profil. Les navigations permettent à EF de résoudre les FK
        //    même quand Member et BureauProfile ne sont pas encore sauvegardés (Ids provisoires).
        await _profiles.AddAssignmentAsync(new MemberBureauProfile
        {
            Member = member,
            BureauProfile = profile,
        }, ct);

        // Un seul SaveChanges : atomicité tout-ou-rien.
        await _profiles.SaveChangesAsync(ct);

        // Post-save : EF a matérialisé les FK. Récupère les droits effectifs et émet le jeton.
        var effective = await _permissions.GetEffectivePermissionsAsync(member.Id, ct);
        var token = _tokenIssuer.Issue(member.Id, member.FullName, effective);

        _audit.Operation("Setup.FirstAdminCreated", new { member.Id, member.Reference });
        return new TokenResponse(token.AccessToken, "Bearer", token.ExpiresAt);
    }
}
