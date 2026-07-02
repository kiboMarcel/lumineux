using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Attendances;
using Lumineux.Application.Contracts.Attendances;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class AddManualAttendanceTests
{
    private static readonly DateTime Now = new(2026, 7, 5, 9, 10, 0, DateTimeKind.Utc);
    private const int MemberId = 55;

    private readonly IAttendanceSessionRepository _sessions = Substitute.For<IAttendanceSessionRepository>();
    private readonly IAttendanceRepository _attendances = Substitute.For<IAttendanceRepository>();
    private readonly IMemberReadRepository _members = Substitute.For<IMemberReadRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private AddManualAttendanceHandler CreateHandler()
    {
        _clock.UtcNow.Returns(Now);
        return new AddManualAttendanceHandler(
            _sessions, _attendances, _members, _clock, _user, _audit, new ManualAttendanceValidator());
    }

    private static AttendanceSession OpenSession()
    {
        var session = AttendanceSession.Start(1, Now.Date, 1, "secret", 30, Now);
        session.Id = 10;
        return session;
    }

    private void GivenBureau() => _user.HasPermission(Permissions.ManageAttendance).Returns(true);

    private void GivenActiveMember() =>
        _members.GetByIdAsync(MemberId, Arg.Any<CancellationToken>())
            .Returns(new Member { Id = MemberId, FirstName = "Al", LastName = "Ba", Status = "Active", AntennaId = 3 });

    private static readonly ManualAttendanceRequest Request = new(MemberId, ArrivalTime: null);

    [Fact]
    public async Task AddManual_creates_manual_attendance()
    {
        GivenBureau();
        GivenActiveMember();
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(OpenSession());
        _attendances.GetValidByMemberAsync(Arg.Any<int>(), MemberId, Arg.Any<CancellationToken>()).Returns((Attendance?)null);

        var result = await CreateHandler().HandleAsync(1, Request);

        result.AlreadyPresent.Should().BeFalse();
        result.Attendance.Source.Should().Be("Manual");
        await _attendances.Received(1).AddAsync(Arg.Any<Attendance>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddManual_without_permission_throws_forbidden()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(false);

        var act = () => CreateHandler().HandleAsync(1, Request);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AddManual_for_unknown_member_throws_not_found()
    {
        GivenBureau();
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(OpenSession());
        _members.GetByIdAsync(MemberId, Arg.Any<CancellationToken>()).Returns((Member?)null);

        var act = () => CreateHandler().HandleAsync(1, Request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddManual_for_inactive_member_throws_forbidden()
    {
        GivenBureau();
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(OpenSession());
        _members.GetByIdAsync(MemberId, Arg.Any<CancellationToken>())
            .Returns(new Member { Id = MemberId, FirstName = "Al", LastName = "Ba", Status = "Inactive" });

        var act = () => CreateHandler().HandleAsync(1, Request);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AddManual_when_already_present_returns_existing()
    {
        GivenBureau();
        GivenActiveMember();
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(OpenSession());
        _attendances.GetValidByMemberAsync(Arg.Any<int>(), MemberId, Arg.Any<CancellationToken>())
            .Returns(Attendance.RecordManual(10, MemberId, Now, 3));

        var result = await CreateHandler().HandleAsync(1, Request);

        result.AlreadyPresent.Should().BeTrue();
        await _attendances.DidNotReceive().AddAsync(Arg.Any<Attendance>(), Arg.Any<CancellationToken>());
    }
}
