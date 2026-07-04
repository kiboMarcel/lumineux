using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.BureauProfiles;
using Lumineux.Application.Contracts.BureauProfiles;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class UpdateBureauProfileTests
{
    private readonly IBureauProfileRepository _profiles = Substitute.For<IBureauProfileRepository>();
    private readonly IPermissionCatalog _catalog = Substitute.For<IPermissionCatalog>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    public UpdateBureauProfileTests()
    {
        _catalog.Contains("manage_attendance").Returns(true);
        _catalog.Contains("manage_members").Returns(true);
        _catalog.Contains("manage_bureau_profiles").Returns(true);
        _user.HasPermission(Permissions.ManageBureauProfiles).Returns(true);
    }

    private UpdateBureauProfileHandler CreateHandler() =>
        new(_profiles, _catalog, _user, _audit, new BureauProfileWriteValidator());

    private BureauProfile ExistingProfile(int id, string name, params string[] perms)
    {
        var p = BureauProfile.Create(name, null, perms, _catalog);
        typeof(AbstractEntity).GetProperty(nameof(AbstractEntity.Id))!.SetValue(p, id);
        return p;
    }

    [Fact]
    public async Task Update_valid_applies_changes()
    {
        var profile = ExistingProfile(1, "Ancien", "manage_attendance");
        _profiles.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(profile);
        _profiles.GetByNameNormalizedAsync("nouveau", Arg.Any<CancellationToken>()).Returns((BureauProfile?)null);
        _profiles.CountAssignmentsAsync(1, Arg.Any<CancellationToken>()).Returns(0);

        var request = new BureauProfileWriteRequest("Nouveau", "desc", new[] { "manage_members" });

        var result = await CreateHandler().HandleAsync(1, request);

        result.Name.Should().Be("Nouveau");
        result.Permissions.Should().ContainSingle().Which.Should().Be("manage_members");
        await _profiles.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_duplicate_name_throws_conflict()
    {
        var profile = ExistingProfile(1, "Ancien", "manage_members");
        var other = ExistingProfile(2, "Existant", "manage_members");
        _profiles.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(profile);
        _profiles.GetByNameNormalizedAsync("existant", Arg.Any<CancellationToken>()).Returns(other);

        var act = () => CreateHandler().HandleAsync(1,
            new BureauProfileWriteRequest("Existant", null, new[] { "manage_members" }));

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("duplicate_name");
    }

    [Fact]
    public async Task Update_removing_admin_from_last_admin_profile_throws_last_administrator()
    {
        var adminProfile = ExistingProfile(1, "Admin", "manage_bureau_profiles");
        _profiles.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(adminProfile);
        _profiles.CountActiveAdministratorsAsync(excludeProfileId: 1, ct: Arg.Any<CancellationToken>()).Returns(0);

        var act = () => CreateHandler().HandleAsync(1,
            new BureauProfileWriteRequest("Admin", null, new[] { "manage_members" }));

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("last_administrator");
    }

    [Fact]
    public async Task Update_removing_admin_but_another_admin_exists_succeeds()
    {
        var adminProfile = ExistingProfile(1, "Admin", "manage_bureau_profiles");
        _profiles.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(adminProfile);
        _profiles.CountActiveAdministratorsAsync(excludeProfileId: 1, ct: Arg.Any<CancellationToken>()).Returns(1);
        _profiles.GetByNameNormalizedAsync("admin", Arg.Any<CancellationToken>()).Returns(adminProfile);

        var result = await CreateHandler().HandleAsync(1,
            new BureauProfileWriteRequest("Admin", null, new[] { "manage_members" }));

        result.Permissions.Should().ContainSingle().Which.Should().Be("manage_members");
    }

    [Fact]
    public async Task Update_unknown_profile_throws_not_found()
    {
        _profiles.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((BureauProfile?)null);

        var act = () => CreateHandler().HandleAsync(99,
            new BureauProfileWriteRequest("X", null, Array.Empty<string>()));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Update_without_permission_throws_forbidden()
    {
        _user.HasPermission(Permissions.ManageBureauProfiles).Returns(false);

        var act = () => new UpdateBureauProfileHandler(_profiles, _catalog, _user, _audit, new BureauProfileWriteValidator())
            .HandleAsync(1, new BureauProfileWriteRequest("X", null, Array.Empty<string>()));

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
