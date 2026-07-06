using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Antennas;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

/// <summary>Activation / désactivation d'antenne (feature 016, US3) — refus si session ouverte.</summary>
public sealed class SetAntennaActiveTests
{
    private readonly IAntennaRepository _antennas = Substitute.For<IAntennaRepository>();
    private readonly IAttendanceSessionRepository _sessions = Substitute.For<IAttendanceSessionRepository>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private SetAntennaActiveHandler Handler() => new(_antennas, _sessions, _user, _audit);

    private Antenna Existing(string status = Antenna.Active)
    {
        var a = Antenna.Create("ANT-01", "Antenne", 1);
        a.Id = 7;
        a.Status = status;
        _antennas.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(a);
        return a;
    }

    [Fact]
    public async Task Deactivates_when_no_open_session()
    {
        _user.HasPermission(Permissions.ManageReferentials).Returns(true);
        Existing();
        _sessions.HasOpenSessionForAntennaAsync(7, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler().HandleAsync(7, active: false);

        result.Status.Should().Be(Antenna.Inactive);
        await _antennas.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refuses_deactivation_when_open_session_exists()
    {
        _user.HasPermission(Permissions.ManageReferentials).Returns(true);
        Existing();
        _sessions.HasOpenSessionForAntennaAsync(7, Arg.Any<CancellationToken>()).Returns(true);

        var act = () => Handler().HandleAsync(7, active: false);

        (await act.Should().ThrowAsync<ConflictException>()).Which.Code.Should().Be("antenna_has_open_sessions");
        await _antennas.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reactivates_inactive_antenna()
    {
        _user.HasPermission(Permissions.ManageReferentials).Returns(true);
        Existing(Antenna.Inactive);

        var result = await Handler().HandleAsync(7, active: true);

        result.Status.Should().Be(Antenna.Active);
        // La réactivation ne dépend pas des sessions ouvertes.
        await _sessions.DidNotReceive().HasOpenSessionForAntennaAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_not_found_for_unknown_antenna()
    {
        _user.HasPermission(Permissions.ManageReferentials).Returns(true);
        _antennas.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Antenna?)null);

        var act = () => Handler().HandleAsync(404, active: false);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Refuses_without_manage_referentials()
    {
        _user.HasPermission(Arg.Any<string>()).Returns(false);

        var act = () => Handler().HandleAsync(7, active: false);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
