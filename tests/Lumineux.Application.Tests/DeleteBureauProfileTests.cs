using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.BureauProfiles;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class DeleteBureauProfileTests
{
    private readonly IBureauProfileRepository _profiles = Substitute.For<IBureauProfileRepository>();
    private readonly IPermissionCatalog _catalog = Substitute.For<IPermissionCatalog>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    public DeleteBureauProfileTests()
    {
        _catalog.Contains(Arg.Any<string>()).Returns(true);
        _user.HasPermission(Permissions.ManageBureauProfiles).Returns(true);
    }

    private DeleteBureauProfileHandler CreateHandler() =>
        new(_profiles, _user, _audit);

    private BureauProfile ExistingProfile(int id, params string[] perms)
    {
        var p = BureauProfile.Create("P" + id, null, perms, _catalog);
        typeof(AbstractEntity).GetProperty(nameof(AbstractEntity.Id))!.SetValue(p, id);
        return p;
    }

    [Fact]
    public async Task Delete_unassigned_non_admin_profile_succeeds()
    {
        var profile = ExistingProfile(1, "manage_attendance");
        _profiles.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(profile);
        _profiles.CountAssignmentsAsync(1, Arg.Any<CancellationToken>()).Returns(0);

        await CreateHandler().HandleAsync(1);

        _profiles.Received().Remove(profile);
        await _profiles.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_assigned_profile_throws_profile_in_use()
    {
        var profile = ExistingProfile(1, "manage_attendance");
        _profiles.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(profile);
        _profiles.CountAssignmentsAsync(1, Arg.Any<CancellationToken>()).Returns(2);

        var act = () => CreateHandler().HandleAsync(1);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("profile_in_use");
    }

    [Fact]
    public async Task Delete_admin_profile_when_last_admin_throws_last_administrator()
    {
        var profile = ExistingProfile(1, "manage_bureau_profiles");
        _profiles.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(profile);
        _profiles.CountAssignmentsAsync(1, Arg.Any<CancellationToken>()).Returns(0);
        _profiles.CountActiveAdministratorsAsync(excludeProfileId: 1, ct: Arg.Any<CancellationToken>()).Returns(0);

        var act = () => CreateHandler().HandleAsync(1);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("last_administrator");
    }

    [Fact]
    public async Task Delete_unknown_profile_throws_not_found()
    {
        _profiles.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((BureauProfile?)null);

        var act = () => CreateHandler().HandleAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_without_permission_throws_forbidden()
    {
        _user.HasPermission(Permissions.ManageBureauProfiles).Returns(false);

        var act = () => CreateHandler().HandleAsync(1);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
