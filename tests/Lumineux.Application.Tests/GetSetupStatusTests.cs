using FluentAssertions;
using Lumineux.Application.Setup;
using Lumineux.Domain.Abstractions;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

/// <summary>
/// Tests unitaires du statut d'installation (feature 012). `Installed` dérive du décompte des
/// administrateurs actifs (même règle que le verrou d'installation).
/// </summary>
public sealed class GetSetupStatusTests
{
    private readonly IBureauProfileRepository _profiles = Substitute.For<IBureauProfileRepository>();

    private GetSetupStatusHandler CreateHandler() => new(_profiles);

    [Fact]
    public async Task Not_installed_when_no_active_administrator()
    {
        _profiles.CountActiveAdministratorsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(0);

        var result = await CreateHandler().HandleAsync();

        result.Installed.Should().BeFalse();
    }

    [Fact]
    public async Task Installed_when_at_least_one_active_administrator()
    {
        _profiles.CountActiveAdministratorsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().HandleAsync();

        result.Installed.Should().BeTrue();
    }
}
