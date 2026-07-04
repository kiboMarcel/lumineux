using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.BureauProfiles;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class RevokeProfileTests
{
    private sealed class OpenCatalog : IPermissionCatalog
    {
        public bool Contains(string permission) => !string.IsNullOrWhiteSpace(permission);
        public IReadOnlyList<PermissionDescriptor> All() => Array.Empty<PermissionDescriptor>();
    }

    private readonly IBureauProfileRepository _profiles = Substitute.For<IBureauProfileRepository>();
    private readonly IPermissionCatalog _catalog = new OpenCatalog();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    public RevokeProfileTests()
    {
        _user.HasPermission(Permissions.ManageBureauProfiles).Returns(true);
    }

    private RevokeProfileHandler CreateHandler() => new(_profiles, _user, _audit);

    private BureauProfile Profile(int id, params string[] perms)
    {
        var p = BureauProfile.Create("P" + id, null, perms, _catalog);
        typeof(AbstractEntity).GetProperty(nameof(AbstractEntity.Id))!.SetValue(p, id);
        return p;
    }

    [Fact]
    public async Task Revoke_valid_removes_assignment()
    {
        var assignment = new MemberBureauProfile { MemberId = 10, BureauProfileId = 1 };
        var profile = Profile(1, "manage_attendance");
        _profiles.GetAssignmentAsync(10, 1, Arg.Any<CancellationToken>()).Returns(assignment);
        _profiles.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(profile);

        await CreateHandler().HandleAsync(10, 1);

        _profiles.Received().RemoveAssignment(assignment);
        await _profiles.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Revoke_missing_assignment_throws_not_found()
    {
        _profiles.GetAssignmentAsync(10, 1, Arg.Any<CancellationToken>())
            .Returns((MemberBureauProfile?)null);

        var act = () => CreateHandler().HandleAsync(10, 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Revoke_last_admin_throws_last_administrator()
    {
        var assignment = new MemberBureauProfile { MemberId = 10, BureauProfileId = 1 };
        var adminProfile = Profile(1, "manage_bureau_profiles");
        _profiles.GetAssignmentAsync(10, 1, Arg.Any<CancellationToken>()).Returns(assignment);
        _profiles.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(adminProfile);
        _profiles.CountActiveAdministratorsAsync(excludeMemberId: 10, ct: Arg.Any<CancellationToken>()).Returns(0);

        var act = () => CreateHandler().HandleAsync(10, 1);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("last_administrator");
    }

    [Fact]
    public async Task Revoke_admin_when_other_admin_exists_succeeds()
    {
        var assignment = new MemberBureauProfile { MemberId = 10, BureauProfileId = 1 };
        var adminProfile = Profile(1, "manage_bureau_profiles");
        _profiles.GetAssignmentAsync(10, 1, Arg.Any<CancellationToken>()).Returns(assignment);
        _profiles.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(adminProfile);
        _profiles.CountActiveAdministratorsAsync(excludeMemberId: 10, ct: Arg.Any<CancellationToken>()).Returns(1);

        await CreateHandler().HandleAsync(10, 1);

        _profiles.Received().RemoveAssignment(assignment);
    }

    [Fact]
    public async Task Revoke_without_permission_throws_forbidden()
    {
        _user.HasPermission(Permissions.ManageBureauProfiles).Returns(false);

        var act = () => CreateHandler().HandleAsync(10, 1);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
