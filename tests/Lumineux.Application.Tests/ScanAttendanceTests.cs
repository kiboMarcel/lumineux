using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Attendances;
using Lumineux.Application.Contracts.Attendances;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class ScanAttendanceTests
{
    private static readonly DateTime Now = new(2026, 7, 5, 9, 5, 0, DateTimeKind.Utc);
    private const int MemberId = 42;

    private readonly IAttendanceSessionRepository _sessions = Substitute.For<IAttendanceSessionRepository>();
    private readonly IAttendanceRepository _attendances = Substitute.For<IAttendanceRepository>();
    private readonly IMemberReadRepository _members = Substitute.For<IMemberReadRepository>();
    private readonly IQrTokenService _qr = Substitute.For<IQrTokenService>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private ScanAttendanceHandler CreateHandler()
    {
        _clock.UtcNow.Returns(Now);
        _user.MemberId.Returns(MemberId);
        return new ScanAttendanceHandler(
            _sessions, _attendances, _members, _qr, _clock, _user, _audit, new ScanRequestValidator());
    }

    private static AttendanceSession OpenSession(int antennaId = 1)
    {
        var session = AttendanceSession.Start(antennaId, Now.Date, openedByMemberId: 1, "secret", 30, Now);
        session.Id = 10; // simule l'id attribué par la base
        return session;
    }

    private void GivenActiveMember(int? originAntenna = 7)
    {
        var member = new Member { Id = MemberId, FirstName = "Jane", LastName = "Doe", Status = "Active", AntennaId = originAntenna };
        _members.GetByIdAsync(MemberId, Arg.Any<CancellationToken>()).Returns(member);
    }

    private static readonly ScanRequest Request = new("12345678");

    [Fact]
    public async Task Scan_records_presence_with_server_time_and_origin_snapshot()
    {
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(OpenSession());
        _qr.Validate(Arg.Any<string>(), Arg.Any<int>(), Request.Token, Now).Returns(true);
        GivenActiveMember(originAntenna: 7);
        _attendances.GetValidByMemberAsync(Arg.Any<int>(), MemberId, Arg.Any<CancellationToken>()).Returns((Attendance?)null);

        Attendance? captured = null;
        await _attendances.AddAsync(Arg.Do<Attendance>(a => captured = a), Arg.Any<CancellationToken>());

        var handler = CreateHandler();
        var result = await handler.HandleAsync(1, Request);

        result.AlreadyPresent.Should().BeFalse();
        result.Attendance.Source.Should().Be("QrScan");
        result.Attendance.ArrivalTime.Should().Be(Now);
        captured!.OriginAntennaId.Should().Be(7);
        await _attendances.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scan_when_already_present_returns_existing_without_duplicate()
    {
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(OpenSession());
        _qr.Validate(Arg.Any<string>(), Arg.Any<int>(), Request.Token, Now).Returns(true);
        GivenActiveMember();
        _attendances.GetValidByMemberAsync(Arg.Any<int>(), MemberId, Arg.Any<CancellationToken>())
            .Returns(Attendance.RecordScan(1, MemberId, Now, 7));

        var handler = CreateHandler();
        var result = await handler.HandleAsync(1, Request);

        result.AlreadyPresent.Should().BeTrue();
        await _attendances.DidNotReceive().AddAsync(Arg.Any<Attendance>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scan_with_invalid_token_throws_gone()
    {
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(OpenSession());
        _qr.Validate(Arg.Any<string>(), Arg.Any<int>(), Request.Token, Now).Returns(false);

        var handler = CreateHandler();
        var act = () => handler.HandleAsync(1, Request);

        await act.Should().ThrowAsync<GoneException>();
    }

    [Fact]
    public async Task Scan_on_closed_session_throws_conflict()
    {
        var session = OpenSession();
        session.Close(1, Now.AddHours(1));
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(session);

        var handler = CreateHandler();
        var act = () => handler.HandleAsync(1, Request);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Scan_by_inactive_member_throws_forbidden()
    {
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(OpenSession());
        _qr.Validate(Arg.Any<string>(), Arg.Any<int>(), Request.Token, Now).Returns(true);
        _members.GetByIdAsync(MemberId, Arg.Any<CancellationToken>())
            .Returns(new Member { Id = MemberId, FirstName = "J", LastName = "D", Status = "Suspended" });

        var handler = CreateHandler();
        var act = () => handler.HandleAsync(1, Request);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
