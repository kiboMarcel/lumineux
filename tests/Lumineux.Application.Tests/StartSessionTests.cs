using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.AttendanceSessions;
using Lumineux.Application.Contracts.Sessions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class StartSessionTests
{
    private static readonly DateTime Now = new(2026, 7, 5, 9, 0, 0, DateTimeKind.Utc);

    private readonly IAttendanceSessionRepository _sessions = Substitute.For<IAttendanceSessionRepository>();
    private readonly IAntennaReadRepository _antennas = Substitute.For<IAntennaReadRepository>();
    private readonly IQrTokenService _qr = Substitute.For<IQrTokenService>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private StartSessionHandler CreateHandler()
    {
        _clock.UtcNow.Returns(Now);
        _qr.GenerateSecret().Returns("c2VjcmV0LWJhc2U2NA==");
        return new StartSessionHandler(_sessions, _antennas, _qr, _clock, _user, _audit, new StartSessionValidator());
    }

    private void GivenAuthorizedBureau()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        _user.MemberId.Returns(42);
    }

    private static StartSessionRequest ValidRequest => new(AntennaId: 1, MeetingDate: Now.Date, QrStepSeconds: 30);

    [Fact]
    public async Task Start_succeeds_and_persists_session()
    {
        GivenAuthorizedBureau();
        _antennas.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _sessions.HasOpenSessionAsync(1, Now.Date, Arg.Any<CancellationToken>()).Returns(false);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(ValidRequest);

        result.Status.Should().Be("Open");
        result.AntennaId.Should().Be(1);
        result.OpenedByMemberId.Should().Be(42);
        await _sessions.Received(1).AddAsync(Arg.Any<AttendanceSession>(), Arg.Any<CancellationToken>());
        await _sessions.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_without_permission_throws_forbidden()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(false);
        var handler = CreateHandler();

        var act = () => handler.HandleAsync(ValidRequest);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _sessions.DidNotReceive().AddAsync(Arg.Any<AttendanceSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_with_unknown_antenna_throws_not_found()
    {
        GivenAuthorizedBureau();
        _antennas.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(false);
        var handler = CreateHandler();

        var act = () => handler.HandleAsync(ValidRequest);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Start_when_open_session_exists_throws_conflict()
    {
        GivenAuthorizedBureau();
        _antennas.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _sessions.HasOpenSessionAsync(1, Now.Date, Arg.Any<CancellationToken>()).Returns(true);
        var handler = CreateHandler();

        var act = () => handler.HandleAsync(ValidRequest);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Start_with_invalid_step_throws_validation()
    {
        GivenAuthorizedBureau();
        var handler = CreateHandler();

        var act = () => handler.HandleAsync(new StartSessionRequest(1, Now.Date, QrStepSeconds: 5));

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
