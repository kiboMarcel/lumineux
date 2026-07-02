using FluentAssertions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Xunit;

namespace Lumineux.Domain.Tests;

public sealed class CloseSessionTests
{
    private static readonly DateTime Now = new(2026, 7, 5, 9, 0, 0, DateTimeKind.Utc);

    private static AttendanceSession Open() => AttendanceSession.Start(1, Now.Date, 1, "secret", 30, Now);

    [Fact]
    public void Close_transitions_open_to_closed_with_actor()
    {
        var session = Open();
        var end = Now.AddHours(2);

        session.Close(closedByMemberId: 7, end);

        session.Status.Should().Be(SessionStatus.Closed);
        session.EndTime.Should().Be(end);
        session.ClosedByMemberId.Should().Be(7);
    }

    [Fact]
    public void Close_from_closed_throws_conflict()
    {
        var session = Open();
        session.Close(7, Now.AddHours(1));

        var act = () => session.Close(7, Now.AddHours(2));

        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void AutoClose_closes_without_actor_and_is_idempotent()
    {
        var session = Open();
        var end = Now.AddHours(3);

        session.AutoClose(end);

        session.Status.Should().Be(SessionStatus.Closed);
        session.EndTime.Should().Be(end);
        session.ClosedByMemberId.Should().BeNull();

        // Idempotent : un second appel ne modifie pas l'heure de fin.
        session.AutoClose(Now.AddHours(9));
        session.EndTime.Should().Be(end);
    }
}
