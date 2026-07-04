using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.BureauProfiles;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class BureauProfileQueryTests
{
    private sealed class OpenCatalog : IPermissionCatalog
    {
        public bool Contains(string permission) => !string.IsNullOrWhiteSpace(permission);
        public IReadOnlyList<PermissionDescriptor> All() => new List<PermissionDescriptor>
        {
            new(Permissions.ManageAttendance, "Presences"),
            new(Permissions.ManageMembers, "Members"),
            new(Permissions.ManageBureauProfiles, "Bureau"),
        };
    }

    private readonly IBureauProfileRepository _profiles = Substitute.For<IBureauProfileRepository>();
    private readonly IMemberRepository _members = Substitute.For<IMemberRepository>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();
    private readonly IPermissionCatalog _catalog = new OpenCatalog();

    private static Member ActiveMember(int id, string reference = "REF-Q")
    {
        var m = new Member
        {
            Reference = reference,
            EntryDate = DateTime.UtcNow,
            Gender = "F",
            LastName = "Q",
            FirstName = "R",
            Status = MemberStatuses.Active,
        };
        typeof(AbstractEntity).GetProperty(nameof(AbstractEntity.Id))!.SetValue(m, id);
        return m;
    }

    private BureauProfile Profile(int id, string name, params string[] perms)
    {
        var p = BureauProfile.Create(name, null, perms, _catalog);
        typeof(AbstractEntity).GetProperty(nameof(AbstractEntity.Id))!.SetValue(p, id);
        return p;
    }

    [Fact]
    public async Task List_with_manage_bureau_profiles_returns_summaries()
    {
        _user.HasPermission(Permissions.ManageBureauProfiles).Returns(true);
        _profiles.ListAllAsync(Arg.Any<CancellationToken>()).Returns(new List<BureauProfile>
        {
            Profile(1, "A", "manage_attendance"),
            Profile(2, "B", "manage_members", "manage_attendance"),
        });
        _profiles.CountAssignmentsByProfileAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, int> { [1] = 3, [2] = 0 });

        var handler = new ListBureauProfilesHandler(_profiles, _user, _audit);
        var result = await handler.HandleAsync();

        result.Should().HaveCount(2);
        result.Single(x => x.Id == 1).MemberCount.Should().Be(3);
        result.Single(x => x.Id == 2).MemberCount.Should().Be(0);
    }

    [Fact]
    public async Task List_with_manage_members_returns_summaries()
    {
        _user.HasPermission(Permissions.ManageBureauProfiles).Returns(false);
        _user.HasPermission(Permissions.ManageMembers).Returns(true);
        _profiles.ListAllAsync(Arg.Any<CancellationToken>()).Returns(new List<BureauProfile>
        {
            Profile(1, "A", "manage_attendance"),
        });
        _profiles.CountAssignmentsByProfileAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, int>());

        var handler = new ListBureauProfilesHandler(_profiles, _user, _audit);
        var result = await handler.HandleAsync();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task List_without_read_permission_throws_forbidden()
    {
        _user.HasPermission(Arg.Any<string>()).Returns(false);

        var handler = new ListBureauProfilesHandler(_profiles, _user, _audit);

        await FluentActions.Awaiting(() => handler.HandleAsync())
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetMemberProfiles_returns_effective_permissions_union()
    {
        _user.HasPermission(Permissions.ManageBureauProfiles).Returns(true);
        _members.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(ActiveMember(10));
        _profiles.GetProfilesForMemberAsync(10, Arg.Any<CancellationToken>())
            .Returns(new List<BureauProfile>
            {
                Profile(1, "A", "manage_attendance"),
                Profile(2, "B", "manage_members", "manage_attendance"),
            });

        var handler = new GetMemberProfilesHandler(_profiles, _members, _user, _audit);
        var result = await handler.HandleAsync(10);

        result.EffectivePermissions.Should().BeEquivalentTo(new[] { "manage_attendance", "manage_members" });
        result.EffectivePermissions.Distinct().Count().Should().Be(result.EffectivePermissions.Count);
        result.Profiles.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMemberProfiles_unknown_member_throws_not_found()
    {
        _user.HasPermission(Permissions.ManageBureauProfiles).Returns(true);
        _members.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Member?)null);

        var handler = new GetMemberProfilesHandler(_profiles, _members, _user, _audit);

        await FluentActions.Awaiting(() => handler.HandleAsync(99))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void ListPermissions_returns_the_fixed_catalog()
    {
        _user.HasPermission(Permissions.ManageBureauProfiles).Returns(true);
        var handler = new ListPermissionsHandler(_catalog, _user, _audit);

        var result = handler.Handle();

        result.Select(x => x.Code).Should().BeEquivalentTo(new[]
        {
            Permissions.ManageAttendance, Permissions.ManageMembers, Permissions.ManageBureauProfiles,
        });
    }
}
