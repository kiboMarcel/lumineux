using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.AttendanceSessions;
using Lumineux.Application.Contracts.Sessions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class CancelSessionTests
{
    private static readonly DateTime Now = new(2026, 7, 5, 12, 0, 0, DateTimeKind.Utc);

    private readonly IAttendanceSessionRepository _sessions = Substitute.For<IAttendanceSessionRepository>();
    private readonly IAttendanceRepository _attendances = Substitute.For<IAttendanceRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    public CancelSessionTests()
    {
        _clock.UtcNow.Returns(Now);
        // La transaction sérialisable exécute simplement l'action (l'isolation réelle est testée en intégration).
        _sessions.ExecuteInSerializableTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<SessionResponse>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<SessionResponse>>>()(CancellationToken.None));
    }

    private CancelSessionHandler CreateHandler() =>
        new(_sessions, _attendances, _clock, _user, _audit);

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
    public async Task Cancel_empty_open_session_sets_cancelled_and_saves()
    {
        GivenBureau();
        var session = OpenSession();
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(session);
        _attendances.CountValidBySessionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(0);

        var response = await CreateHandler().HandleAsync(10);

        response.Status.Should().Be("Cancelled");
        session.Status.Should().Be(SessionStatus.Cancelled);
        session.CancelledByMemberId.Should().Be(7);
        session.CancelledAt.Should().Be(Now);
        await _sessions.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _audit.Received(1).Operation("CancelSession", Arg.Any<object>());
    }

    [Fact]
    public async Task Cancel_not_found_throws_notfound()
    {
        GivenBureau();
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((AttendanceSession?)null);

        var act = () => CreateHandler().HandleAsync(10);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Cancel_closed_session_throws_conflict()
    {
        GivenBureau();
        var session = OpenSession();
        session.Close(7, Now.AddHours(-1));
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(session);

        var act = () => CreateHandler().HandleAsync(10);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Cancel_session_with_valid_presences_is_refused_without_saving()
    {
        GivenBureau();
        var session = OpenSession();
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(session);
        _attendances.CountValidBySessionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(1);

        var act = () => CreateHandler().HandleAsync(10);

        await act.Should().ThrowAsync<ConflictException>();
        session.Status.Should().Be(SessionStatus.Open); // inchangée
        await _sessions.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        _audit.Received(1).Refused("CancelSession", Arg.Any<string>(), Arg.Any<object>());
    }

    [Fact]
    public async Task Cancel_is_allowed_when_only_presence_was_cancelled_count_zero()
    {
        // Une présence ajoutée puis annulée → décompte valide = 0 → annulation autorisée (Edge Case).
        GivenBureau();
        var session = OpenSession();
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(session);
        _attendances.CountValidBySessionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(0);

        var response = await CreateHandler().HandleAsync(10);

        response.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Cancel_without_permission_throws_forbidden_and_is_logged()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(false);

        var act = () => CreateHandler().HandleAsync(10);

        await act.Should().ThrowAsync<ForbiddenException>();
        _audit.Received(1).Refused("CancelSession", Arg.Any<string>(), Arg.Any<object>());
    }
}
