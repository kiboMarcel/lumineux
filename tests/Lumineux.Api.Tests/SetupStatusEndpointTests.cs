using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

/// <summary>
/// Tests d'intégration du statut d'installation (feature 012). Endpoint anonyme, réponse booléenne,
/// bascule cohérente avec le verrou d'installation.
/// </summary>
public sealed class SetupStatusEndpointTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public SetupStatusEndpointTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Status_is_accessible_anonymously_and_returns_only_installed()
    {
        var response = await _fixture.CreateClient().GetAsync("/api/v1/setup/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        // Réponse strictement booléenne : une seule propriété « installed » (SC-003).
        doc.RootElement.EnumerateObject().Should().ContainSingle();
        doc.RootElement.TryGetProperty("installed", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Status_toggles_from_false_to_true_after_installing_first_admin()
    {
        await _fixture.ResetInstallationStateAsync();
        var client = _fixture.CreateClient();

        (await ReadInstalled(client)).Should().BeFalse();

        var install = await client.PostAsJsonAsync("/api/v1/setup/first-admin",
            new { lastName = "Root", firstName = "Admin", gender = "M", password = "Passw0rd" });
        install.StatusCode.Should().Be(HttpStatusCode.Created);

        (await ReadInstalled(client)).Should().BeTrue();
    }

    private static async Task<bool> ReadInstalled(HttpClient client)
    {
        var raw = await (await client.GetAsync("/api/v1/setup/status")).Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement.GetProperty("installed").GetBoolean();
    }
}
