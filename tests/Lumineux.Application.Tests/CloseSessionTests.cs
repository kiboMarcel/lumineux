using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.AttendanceSessions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class CloseSessionTests
{
    private static readonly DateTime Now = new(2026, 7, 5, 12, 0, 0, DateTimeKind.Utc);

    private readonly IAttendanceSessionRepository _sessions = Substitute.For<IAttendanceSessionRepository>();
    private readonly IAttendanceRepository _attendances = Substitute.For<IAttendanceRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private CloseSessionHandler CreateHandler()
    {
        _clock.UtcNow.Returns(Now);
        return new CloseSessionHandler(_sessions, _attendances, _clock, _user, _audit);
    }

    private static AttendanceSession OpenSession()
    {
        var session = AttendanceSession.Start(1, Now.Date, 1, "secret", 30, Now.AddHours(-2));
        session.Id = 10;
        return session;
    }

    private void GivenBureau()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        _user.MemberId.Returns(7);
    }

    [Fact]
    public async Task Close_sets_end_time_and_propagates_to_all_valid_attendances()
    {
        GivenBureau();
        var session = OpenSession();
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(session);

        var a1 = Attendance.RecordScan(10, 1, Now.AddHours(-1), null);
        var a2 = Attendance.RecordManual(10, 2, Now.AddHours(-1), null);
        _attendances.GetValidBySessionForUpdateAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Attendance> { a1, a2 });

        var response = await CreateHandler().HandleAsync(1);

        response.Status.Should().Be("Closed");
        response.EndTime.Should().Be(Now);
        session.Status.Should().Be(SessionStatus.Closed);
        a1.EndTime.Should().Be(Now);
        a2.EndTime.Should().Be(Now);
        await _sessions.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_already_closed_session_throws_conflict()
    {
        GivenBureau();
        var session = OpenSession();
        session.Close(7, Now.AddHours(-1));
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(session);

        var act = () => CreateHandler().HandleAsync(1);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Close_without_permission_throws_forbidden()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(false);

        var act = () => CreateHandler().HandleAsync(1);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
