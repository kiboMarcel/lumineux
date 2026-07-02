using FluentAssertions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Xunit;

namespace Lumineux.Domain.Tests;

public sealed class AttendanceSessionTests
{
    private static readonly DateTime Now = new(2026, 7, 5, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Start_creates_open_session_with_server_time()
    {
        var session = AttendanceSession.Start(1, Now.Date, openedByMemberId: 42, "secret", 30, Now);

        session.Status.Should().Be(SessionStatus.Open);
        session.IsOpen.Should().BeTrue();
        session.StartTime.Should().Be(Now);
        session.AntennaId.Should().Be(1);
        session.OpenedByMemberId.Should().Be(42);
        session.EndTime.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Start_with_invalid_antenna_throws(int antennaId)
    {
        var act = () => AttendanceSession.Start(antennaId, Now.Date, 42, "secret", 30, Now);
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(121)]
    public void Start_with_invalid_step_throws(int step)
    {
        var act = () => AttendanceSession.Start(1, Now.Date, 42, "secret", step, Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Start_with_empty_secret_throws()
    {
        var act = () => AttendanceSession.Start(1, Now.Date, 42, "  ", 30, Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Close_sets_end_time_and_closed_state()
    {
        var session = AttendanceSession.Start(1, Now.Date, 42, "secret", 30, Now);
        var closeTime = Now.AddHours(2);

        session.Close(closedByMemberId: 7, closeTime);

        session.Status.Should().Be(SessionStatus.Closed);
        session.EndTime.Should().Be(closeTime);
        session.ClosedByMemberId.Should().Be(7);
    }

    [Fact]
    public void Close_when_already_closed_throws_conflict()
    {
        var session = AttendanceSession.Start(1, Now.Date, 42, "secret", 30, Now);
        session.Close(7, Now.AddHours(1));

        var act = () => session.Close(7, Now.AddHours(2));
        act.Should().Throw<ConflictException>();
    }
}
