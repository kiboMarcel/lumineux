using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Setup;
using Lumineux.Application.Setup;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class InstallFirstAdminTests
{
    private sealed class OpenCatalog : IPermissionCatalog
    {
        public bool Contains(string permission) => !string.IsNullOrWhiteSpace(permission);
        public IReadOnlyList<PermissionDescriptor> All() => new List<PermissionDescriptor>
        {
            new(Permissions.ManageAttendance, "Att"),
            new(Permissions.ManageMembers, "Mem"),
            new(Permissions.ManageBureauProfiles, "Bur"),
        };
    }

    private static readonly DateTime Now = new(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc);

    private readonly IBureauProfileRepository _profiles = Substitute.For<IBureauProfileRepository>();
    private readonly IMemberRepository _members = Substitute.For<IMemberRepository>();
    private readonly IMemberAccountRepository _accounts = Substitute.For<IMemberAccountRepository>();
    private readonly IMemberReferenceGenerator _referenceGen = Substitute.For<IMemberReferenceGenerator>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IPermissionCatalog _catalog = new OpenCatalog();
    private readonly IMemberPermissionRepository _permissions = Substitute.For<IMemberPermissionRepository>();
    private readonly ITokenIssuer _tokenIssuer = Substitute.For<ITokenIssuer>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private InstallFirstAdminHandler CreateHandler()
    {
        _clock.UtcNow.Returns(Now);
        _hasher.Hash(Arg.Any<string>()).Returns(ci => "hash:" + ci.Arg<string>());
        _referenceGen.NextAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("LUM-2026-00001");
        _tokenIssuer.Issue(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>())
            .Returns(new IssuedToken("access-token", Now.AddMinutes(60)));
        _permissions.GetPermissionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>
            {
                Permissions.ManageAttendance, Permissions.ManageMembers, Permissions.ManageBureauProfiles,
            });
        return new InstallFirstAdminHandler(_profiles, _members, _accounts, _referenceGen, _hasher,
            _catalog, _permissions, _tokenIssuer, _clock, _audit,
            new InstallFirstAdminValidator(Options.Create(new AuthOptions())));
    }

    private static readonly InstallFirstAdminRequest ValidRequest = new(
        LastName: "Kouassi", FirstName: "Yao", Gender: "M",
        Password: "MotDePasse1", Email: "yao@example.com", Mobile: null);

    [Fact]
    public async Task Install_valid_creates_member_account_profile_assignment_and_returns_token()
    {
        _profiles.CountActiveAdministratorsAsync(ct: Arg.Any<CancellationToken>()).Returns(0);
        _profiles.GetByNameNormalizedAsync("administrateur", Arg.Any<CancellationToken>())
            .Returns((BureauProfile?)null);
        _members.IsContactUsedByActiveAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().HandleAsync(ValidRequest);

        result.AccessToken.Should().Be("access-token");
        result.TokenType.Should().Be("Bearer");
        await _members.Received().AddAsync(Arg.Is<Member>(m => m.Reference == "LUM-2026-00001" && m.IsActive), Arg.Any<CancellationToken>());
        await _accounts.Received().AddAsync(Arg.Is<MemberAccount>(a => a.MustChangePassword == false), Arg.Any<CancellationToken>());
        await _profiles.Received().AddAsync(Arg.Any<BureauProfile>(), Arg.Any<CancellationToken>());
        await _profiles.Received().AddAssignmentAsync(Arg.Any<MemberBureauProfile>(), Arg.Any<CancellationToken>());
        await _profiles.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>()); // atomicité : UN SEUL SaveChanges
    }

    [Fact]
    public async Task Install_when_admin_exists_returns_conflict_already_installed_priority_over_validation()
    {
        _profiles.CountActiveAdministratorsAsync(ct: Arg.Any<CancellationToken>()).Returns(1);

        // Payload volontairement INVALIDE (mot de passe faible) : le refus prioritaire doit gagner.
        var invalidRequest = ValidRequest with { Password = "weak", LastName = "" };

        var act = () => CreateHandler().HandleAsync(invalidRequest);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("already_installed");
        // Le validator NE DOIT PAS être appelé (aucune exception de validation levée).
        await _members.DidNotReceive().AddAsync(Arg.Any<Member>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Install_with_weak_password_when_no_admin_returns_validation_error()
    {
        _profiles.CountActiveAdministratorsAsync(ct: Arg.Any<CancellationToken>()).Returns(0);

        var act = () => CreateHandler().HandleAsync(ValidRequest with { Password = "weak" });

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        await _members.DidNotReceive().AddAsync(Arg.Any<Member>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Install_with_existing_contact_returns_contact_in_use()
    {
        _profiles.CountActiveAdministratorsAsync(ct: Arg.Any<CancellationToken>()).Returns(0);
        _members.IsContactUsedByActiveAsync("yao@example.com", null, null, Arg.Any<CancellationToken>())
            .Returns(true);

        var act = () => CreateHandler().HandleAsync(ValidRequest);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("contact_in_use");
        await _accounts.DidNotReceive().AddAsync(Arg.Any<MemberAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Install_reuses_existing_admin_profile_without_modifying_it()
    {
        _profiles.CountActiveAdministratorsAsync(ct: Arg.Any<CancellationToken>()).Returns(0);
        _members.IsContactUsedByActiveAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Profil existant portant SEULEMENT manage_bureau_profiles (droits partiels).
        var existing = BureauProfile.Create("Administrateur", "Existing description",
            new[] { Permissions.ManageBureauProfiles }, _catalog);
        _profiles.GetByNameNormalizedAsync("administrateur", Arg.Any<CancellationToken>()).Returns(existing);

        await CreateHandler().HandleAsync(ValidRequest);

        // AUCUN nouveau profil créé.
        await _profiles.DidNotReceive().AddAsync(Arg.Any<BureauProfile>(), Arg.Any<CancellationToken>());
        // Les droits du profil existant sont INCHANGÉS (aucun ajout).
        existing.Permissions.Should().ContainSingle().Which.Permission.Should().Be(Permissions.ManageBureauProfiles);
        existing.Description.Should().Be("Existing description");
    }

    [Fact]
    public async Task Install_when_save_fails_does_not_issue_token()
    {
        _profiles.CountActiveAdministratorsAsync(ct: Arg.Any<CancellationToken>()).Returns(0);
        _members.IsContactUsedByActiveAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _profiles.GetByNameNormalizedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((BureauProfile?)null);
        _profiles.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("SaveChanges failed"));

        var act = () => CreateHandler().HandleAsync(ValidRequest);

        await act.Should().ThrowAsync<InvalidOperationException>();
        // Le jeton NE DOIT PAS être émis si le save a échoué (atomicité — SC-003).
        _tokenIssuer.DidNotReceive().Issue(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>());
    }
}
