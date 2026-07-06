using FluentAssertions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Xunit;

namespace Lumineux.Domain.Tests;

/// <summary>Invariants et transitions d'état de l'entité Antenne (feature 016).</summary>
public sealed class AntennaTests
{
    [Fact]
    public void Create_builds_active_antenna_and_trims_code()
    {
        var antenna = Antenna.Create("  ANT-01 ", "  Antenne 1 ", districtId: 3);

        antenna.Code.Should().Be("ANT-01");
        antenna.Label.Should().Be("Antenne 1");
        antenna.District.Should().Be(3);
        antenna.Status.Should().Be(Antenna.Active);
        antenna.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Libellé", 1)]
    [InlineData("ANT", "", 1)]
    [InlineData("ANT", "Libellé", 0)]
    public void Create_rejects_invalid_input(string code, string label, int districtId)
    {
        var act = () => Antenna.Create(code, label, districtId);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateDetails_changes_label_and_district_but_not_code()
    {
        var antenna = Antenna.Create("ANT-01", "Ancien", 1);

        antenna.UpdateDetails("Nouveau", 5);

        antenna.Code.Should().Be("ANT-01"); // immuable
        antenna.Label.Should().Be("Nouveau");
        antenna.District.Should().Be(5);
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData("Libellé", 0)]
    public void UpdateDetails_rejects_invalid_input(string label, int districtId)
    {
        var antenna = Antenna.Create("ANT-01", "Antenne", 1);
        var act = () => antenna.UpdateDetails(label, districtId);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deactivate_then_activate_toggles_status_idempotently()
    {
        var antenna = Antenna.Create("ANT-01", "Antenne", 1);

        antenna.Deactivate();
        antenna.Deactivate(); // idempotent
        antenna.Status.Should().Be(Antenna.Inactive);
        antenna.IsActive.Should().BeFalse();

        antenna.Activate();
        antenna.Activate(); // idempotent
        antenna.Status.Should().Be(Antenna.Active);
    }
}
