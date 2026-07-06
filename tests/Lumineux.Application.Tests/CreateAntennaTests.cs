using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Antennas;
using Lumineux.Application.Contracts.Antennas;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

/// <summary>Cas d'usage de création d'antenne (feature 016, US1).</summary>
public sealed class CreateAntennaTests
{
    private readonly IAntennaRepository _antennas = Substitute.For<IAntennaRepository>();
    private readonly IReferenceLookupRepository _lookup = Substitute.For<IReferenceLookupRepository>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private CreateAntennaHandler Handler() =>
        new(_antennas, _lookup, _user, _audit, new CreateAntennaValidator());

    private static CreateAntennaRequest Request() => new("ANT-01", "Antenne 1", DistrictId: 1);

    [Fact]
    public async Task Creates_active_antenna_when_code_unique_and_district_exists()
    {
        _user.HasPermission(Permissions.ManageReferentials).Returns(true);
        _lookup.DistrictExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _antennas.GetByCodeAsync("ANT-01", Arg.Any<CancellationToken>()).Returns((Antenna?)null);

        var result = await Handler().HandleAsync(Request());

        result.Code.Should().Be("ANT-01");
        result.Status.Should().Be(Antenna.Active);
        await _antennas.Received(1).AddAsync(Arg.Any<Antenna>(), Arg.Any<CancellationToken>());
        await _antennas.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_duplicate_code()
    {
        _user.HasPermission(Permissions.ManageReferentials).Returns(true);
        _lookup.DistrictExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _antennas.GetByCodeAsync("ANT-01", Arg.Any<CancellationToken>()).Returns(Antenna.Create("ANT-01", "Existante", 1));

        var act = () => Handler().HandleAsync(Request());

        (await act.Should().ThrowAsync<ConflictException>()).Which.Code.Should().Be("duplicate_code");
        await _antennas.DidNotReceive().AddAsync(Arg.Any<Antenna>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_unknown_district()
    {
        _user.HasPermission(Permissions.ManageReferentials).Returns(true);
        _lookup.DistrictExistsAsync(1, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => Handler().HandleAsync(Request());

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Refuses_without_manage_referentials()
    {
        _user.HasPermission(Arg.Any<string>()).Returns(false);

        var act = () => Handler().HandleAsync(Request());

        await act.Should().ThrowAsync<ForbiddenException>();
        await _antennas.DidNotReceive().AddAsync(Arg.Any<Antenna>(), Arg.Any<CancellationToken>());
    }
}
