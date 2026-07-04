using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.BureauProfiles;
using Lumineux.Application.Contracts.BureauProfiles;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class AssignProfileTests
{
    private sealed class OpenCatalog : IPermissionCatalog
    {
        public bool Contains(string permission) => !string.IsNullOrWhiteSpace(permission);
        public IReadOnlyList<PermissionDescriptor> All() => Array.Empty<PermissionDescriptor>();
    }

    private readonly IBureauProfileRepository _profiles = Substitute.For<IBureauProfileRepository>();
    private readonly IMemberRepository _members = Substitute.For<IMemberRepository>();
    private readonly IPermissionCatalog _catalog = new OpenCatalog();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    public AssignProfileTests()
    {
        _user.HasPermission(Permissions.ManageBureauProfiles).Returns(true);
    }

    private AssignProfileHandler CreateHandler() =>
        new(_profiles, _members, _user, _audit, new AssignProfileValidator());

    private static Member ActiveMember(int id)
    {
        var m = Member.Create("REF-" + id, DateTime.UtcNow, "Doe", "Jane", "F", 1);
        m.Status = MemberStatuses.Active;
        typeof(AbstractEntity).GetProperty(nameof(AbstractEntity.Id))!.SetValue(m, id);
        return m;
    }

    private BureauProfile Profile(int id)
    {
        var p = BureauProfile.Create("P" + id, null, new[] { "manage_attendance" }, _catalog);
        typeof(AbstractEntity).GetProperty(nameof(AbstractEntity.Id))!.SetValue(p, id);
        return p;
    }

    [Fact]
    public async Task Assign_valid_adds_assignment()
    {
        _members.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(ActiveMember(10));
        _profiles.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Profile(1));
        _profiles.GetAssignmentAsync(10, 1, Arg.Any<CancellationToken>()).Returns((MemberBureauProfile?)null);

        await CreateHandler().HandleAsync(10, new AssignProfileRequest(1));

        await _profiles.Received().AddAssignmentAsync(
            Arg.Is<MemberBureauProfile>(x => x.MemberId == 10 && x.BureauProfileId == 1),
            Arg.Any<CancellationToken>());
        await _profiles.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Assign_idempotent_when_already_assigned()
    {
        _members.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(ActiveMember(10));
        _profiles.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Profile(1));
        _profiles.GetAssignmentAsync(10, 1, Arg.Any<CancellationToken>())
            .Returns(new MemberBureauProfile { MemberId = 10, BureauProfileId = 1 });

        await CreateHandler().HandleAsync(10, new AssignProfileRequest(1));

        await _profiles.DidNotReceive().AddAssignmentAsync(Arg.Any<MemberBureauProfile>(), Arg.Any<CancellationToken>());
        await _profiles.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Assign_inactive_member_throws_member_inactive()
    {
        var m = ActiveMember(10);
        m.Status = "Archived";
        _members.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(m);
        _profiles.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Profile(1));

        var act = () => CreateHandler().HandleAsync(10, new AssignProfileRequest(1));

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("member_inactive");
    }

    [Fact]
    public async Task Assign_unknown_profile_throws_not_found()
    {
        _members.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(ActiveMember(10));
        _profiles.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((BureauProfile?)null);

        var act = () => CreateHandler().HandleAsync(10, new AssignProfileRequest(99));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Assign_unknown_member_throws_not_found()
    {
        _members.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Member?)null);

        var act = () => CreateHandler().HandleAsync(99, new AssignProfileRequest(1));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Assign_without_permission_throws_forbidden()
    {
        _user.HasPermission(Permissions.ManageBureauProfiles).Returns(false);

        var act = () => CreateHandler().HandleAsync(10, new AssignProfileRequest(1));

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
