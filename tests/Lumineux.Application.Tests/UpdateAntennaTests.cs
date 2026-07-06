using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Antennas;
using Lumineux.Application.Contracts.Antennas;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

/// <summary>Cas d'usage de modification d'antenne (feature 016, US2) — code immuable.</summary>
public sealed class UpdateAntennaTests
{
    private readonly IAntennaRepository _antennas = Substitute.For<IAntennaRepository>();
    private readonly IReferenceLookupRepository _lookup = Substitute.For<IReferenceLookupRepository>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private UpdateAntennaHandler Handler() =>
        new(_antennas, _lookup, _user, _audit, new UpdateAntennaValidator());

    private static Antenna Existing()
    {
        var a = Antenna.Create("ANT-01", "Ancien", 1);
        a.Id = 7;
        return a;
    }

    [Fact]
    public async Task Updates_label_and_district_without_touching_code()
    {
        _user.HasPermission(Permissions.ManageReferentials).Returns(true);
        var antenna = Existing();
        _antennas.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(antenna);
        _lookup.DistrictExistsAsync(5, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Handler().HandleAsync(7, new UpdateAntennaRequest("Nouveau", 5));

        result.Code.Should().Be("ANT-01"); // immuable
        result.Label.Should().Be("Nouveau");
        result.DistrictId.Should().Be(5);
        await _antennas.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_unknown_district()
    {
        _user.HasPermission(Permissions.ManageReferentials).Returns(true);
        _antennas.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(Existing());
        _lookup.DistrictExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(false);

        var act = () => Handler().HandleAsync(7, new UpdateAntennaRequest("Nouveau", 99));

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Returns_not_found_for_unknown_antenna()
    {
        _user.HasPermission(Permissions.ManageReferentials).Returns(true);
        _antennas.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Antenna?)null);

        var act = () => Handler().HandleAsync(404, new UpdateAntennaRequest("X", 1));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Refuses_without_manage_referentials()
    {
        _user.HasPermission(Arg.Any<string>()).Returns(false);

        var act = () => Handler().HandleAsync(7, new UpdateAntennaRequest("X", 1));

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
