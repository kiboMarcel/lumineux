using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Attendances;
using Lumineux.Application.Contracts.Attendances;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class SyncOfflineScansTests
{
    private static readonly DateTime Now = new(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc);
    private const int MemberId = 42;

    private readonly IAttendanceSessionRepository _sessions = Substitute.For<IAttendanceSessionRepository>();
    private readonly IAttendanceRepository _attendances = Substitute.For<IAttendanceRepository>();
    private readonly IMemberReadRepository _members = Substitute.For<IMemberReadRepository>();
    private readonly IQrTokenService _qr = Substitute.For<IQrTokenService>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private SyncOfflineScansHandler CreateHandler()
    {
        _clock.UtcNow.Returns(Now);
        _user.MemberId.Returns(MemberId);
        _members.GetByIdAsync(MemberId, Arg.Any<CancellationToken>())
            .Returns(new Member { Id = MemberId, FirstName = "J", LastName = "D", Status = "Active", AntennaId = 7 });
        return new SyncOfflineScansHandler(
            _sessions, _attendances, _members, _qr, _clock, _user, _audit, new OfflineScanBatchValidator());
    }

    private static AttendanceSession OpenSession()
    {
        var session = AttendanceSession.Start(1, Now.Date, 1, "secret", 30, Now.AddMinutes(-30));
        session.Id = 10; // simule l'id attribué par la base
        return session;
    }

    private static OfflineScanBatchRequest Batch(DateTime arrival, string token = "12345678", string op = "op-1") =>
        new([new OfflineScanItem(op, token, arrival)]);

    [Fact]
    public async Task Sync_creates_attendance_with_client_arrival_time()
    {
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(OpenSession());
        _qr.Validate(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<DateTime>()).Returns(true);
        _attendances.GetByClientOperationIdAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Attendance?)null);
        _attendances.GetValidByMemberAsync(Arg.Any<int>(), MemberId, Arg.Any<CancellationToken>()).Returns((Attendance?)null);

        var handler = CreateHandler();
        var response = await handler.HandleAsync(1, Batch(Now.AddMinutes(-5)));

        response.Results.Should().ContainSingle()
            .Which.Outcome.Should().Be(OfflineScanOutcome.Created);
        await _attendances.Received(1).AddAsync(Arg.Any<Attendance>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sync_is_idempotent_on_client_operation_id()
    {
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(OpenSession());
        _attendances.GetByClientOperationIdAsync(Arg.Any<int>(), "op-1", Arg.Any<CancellationToken>())
            .Returns(Attendance.RecordScan(1, MemberId, Now.AddMinutes(-5), 7, "op-1"));

        var handler = CreateHandler();
        var response = await handler.HandleAsync(1, Batch(Now.AddMinutes(-5)));

        response.Results.Single().Outcome.Should().Be(OfflineScanOutcome.AlreadyPresent);
        await _attendances.DidNotReceive().AddAsync(Arg.Any<Attendance>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sync_rejects_invalid_token()
    {
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(OpenSession());
        _qr.Validate(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<DateTime>()).Returns(false);
        _attendances.GetByClientOperationIdAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Attendance?)null);

        var handler = CreateHandler();
        var response = await handler.HandleAsync(1, Batch(Now.AddMinutes(-5)));

        response.Results.Single().Outcome.Should().Be(OfflineScanOutcome.Rejected);
    }

    [Fact]
    public async Task Sync_rejects_arrival_after_close()
    {
        var closed = OpenSession();
        closed.Close(1, Now.AddMinutes(-10));
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(closed);
        _qr.Validate(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<DateTime>()).Returns(true);
        _attendances.GetByClientOperationIdAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Attendance?)null);

        var handler = CreateHandler();
        // arrivée à Now-5min, postérieure à la clôture (Now-10min) → rejet (FR-023b)
        var response = await handler.HandleAsync(1, Batch(Now.AddMinutes(-5)));

        var result = response.Results.Single();
        result.Outcome.Should().Be(OfflineScanOutcome.Rejected);
        result.Reason.Should().Contain("clôture");
    }
}
