using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Antennas;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

/// <summary>Liste de gestion des antennes (feature 016, US4) — inactives incluses.</summary>
public sealed class ListAntennasTests
{
    private readonly IAntennaRepository _antennas = Substitute.For<IAntennaRepository>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();

    private ListAntennasHandler Handler() => new(_antennas, _user);

    [Fact]
    public async Task Returns_active_and_inactive_with_status()
    {
        _user.HasPermission(Permissions.ManageReferentials).Returns(true);
        var active = Antenna.Create("ANT-01", "Active", 1);
        var inactive = Antenna.Create("ANT-02", "Inactive", 1);
        inactive.Deactivate();
        _antennas.ListAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Antenna> { active, inactive });

        var result = await Handler().HandleAsync();

        result.Should().HaveCount(2);
        result.Select(a => a.Status).Should().Contain(new[] { Antenna.Active, Antenna.Inactive });
    }

    [Fact]
    public async Task Refuses_without_manage_referentials()
    {
        _user.HasPermission(Arg.Any<string>()).Returns(false);

        var act = () => Handler().HandleAsync();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
