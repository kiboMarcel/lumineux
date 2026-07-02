using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Attendances;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class CancelAttendanceTests
{
    private static readonly DateTime Now = new(2026, 7, 5, 9, 15, 0, DateTimeKind.Utc);
    private const int MemberId = 55;

    private readonly IAttendanceSessionRepository _sessions = Substitute.For<IAttendanceSessionRepository>();
    private readonly IAttendanceRepository _attendances = Substitute.For<IAttendanceRepository>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private CancelAttendanceHandler CreateHandler() =>
        new(_sessions, _attendances, _user, _audit);

    private static AttendanceSession OpenSession()
    {
        var session = AttendanceSession.Start(1, Now.Date, 1, "secret", 30, Now);
        session.Id = 10;
        return session;
    }

    [Fact]
    public async Task Cancel_marks_attendance_cancelled_and_saves()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(OpenSession());
        var attendance = Attendance.RecordManual(10, MemberId, Now, 3);
        _attendances.GetValidByMemberAsync(Arg.Any<int>(), MemberId, Arg.Any<CancellationToken>()).Returns(attendance);

        await CreateHandler().HandleAsync(1, MemberId);

        attendance.Status.Should().Be(AttendanceStatus.Cancelled);
        await _attendances.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_without_permission_throws_forbidden()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(false);

        var act = () => CreateHandler().HandleAsync(1, MemberId);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Cancel_on_closed_session_throws_conflict()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        var closed = OpenSession();
        closed.Close(1, Now.AddHours(1));
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(closed);

        var act = () => CreateHandler().HandleAsync(1, MemberId);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Cancel_when_attendance_missing_throws_not_found()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(OpenSession());
        _attendances.GetValidByMemberAsync(Arg.Any<int>(), MemberId, Arg.Any<CancellationToken>()).Returns((Attendance?)null);

        var act = () => CreateHandler().HandleAsync(1, MemberId);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
