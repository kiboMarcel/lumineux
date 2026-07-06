using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.AttendanceSessions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

/// <summary>Cas d'usage : mes sessions de présence ouvertes (feature 023).</summary>
public sealed class ListMyOpenSessionsTests
{
    private static readonly DateTime Now = new(2026, 7, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly IAttendanceSessionRepository _sessions = Substitute.For<IAttendanceSessionRepository>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();

    private ListMyOpenSessionsHandler Handler() => new(_sessions, _user);

    private static AttendanceSession OpenSession(int openedBy)
    {
        var s = AttendanceSession.Start(1, Now.Date, openedBy, "secret", 30, Now);
        s.Id = 55;
        return s;
    }

    [Fact]
    public async Task Returns_current_user_open_sessions()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        _user.MemberId.Returns(42);
        _sessions.ListOpenByOpenerAsync(42, Arg.Any<CancellationToken>())
            .Returns(new List<AttendanceSession> { OpenSession(42) });

        var result = await Handler().HandleAsync();

        result.Should().ContainSingle().Which.Id.Should().Be(55);
        // L'initiateur interrogé est bien l'utilisateur courant (jeton), pas un paramètre client.
        await _sessions.Received(1).ListOpenByOpenerAsync(42, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_empty_list_when_no_open_session()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        _user.MemberId.Returns(42);
        _sessions.ListOpenByOpenerAsync(42, Arg.Any<CancellationToken>())
            .Returns(new List<AttendanceSession>());

        (await Handler().HandleAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_empty_when_token_has_no_member()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        _user.MemberId.Returns((int?)null);

        (await Handler().HandleAsync()).Should().BeEmpty();
        await _sessions.DidNotReceive().ListOpenByOpenerAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refuses_without_manage_attendance()
    {
        _user.HasPermission(Arg.Any<string>()).Returns(false);

        var act = () => Handler().HandleAsync();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
