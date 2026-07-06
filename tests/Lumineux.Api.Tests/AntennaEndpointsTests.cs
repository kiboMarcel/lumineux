using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

/// <summary>Tests d'intégration de la gestion des antennes (feature 016) : CRUD, RBAC, règles métier.</summary>
public sealed class AntennaEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public AntennaEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    private HttpClient ClientWith(string token)
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string UniqueCode() => "ANT-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    private object CreateBody(string code) => new { code, label = "Antenne " + code, districtId = _fixture.SeededDistrictId };

    [Fact]
    public async Task Create_returns_201_with_location_and_active_status()
    {
        var client = ClientWith(_fixture.IssueReferentialsManagerToken());

        var response = await client.PostAsJsonAsync("/api/v1/antennas", CreateBody(UniqueCode()));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("status").GetString().Should().Be("Active");
    }

    [Fact]
    public async Task Duplicate_code_returns_409_with_code()
    {
        var client = ClientWith(_fixture.IssueReferentialsManagerToken());
        var code = UniqueCode();
        (await client.PostAsJsonAsync("/api/v1/antennas", CreateBody(code))).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/v1/antennas", CreateBody(code));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("duplicate_code");
    }

    [Fact]
    public async Task Without_token_returns_401()
    {
        var response = await _fixture.CreateClient().GetAsync("/api/v1/antennas");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Without_manage_referentials_returns_403()
    {
        var response = await ClientWith(_fixture.IssueMemberToken()).GetAsync("/api/v1/antennas");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Full_lifecycle_create_update_deactivate_activate_and_list()
    {
        var client = ClientWith(_fixture.IssueReferentialsManagerToken());
        var code = UniqueCode();

        var created = await (await client.PostAsJsonAsync("/api/v1/antennas", CreateBody(code))).Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        // Modifier (code immuable côté serveur)
        var updated = await client.PutAsJsonAsync($"/api/v1/antennas/{id}", new { label = "Renommée", districtId = _fixture.SeededDistrictId });
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        (await updated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("label").GetString().Should().Be("Renommée");

        // Désactiver → disparaît de la lecture publique 010
        (await client.PostAsync($"/api/v1/antennas/{id}/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var publicList = await client.GetFromJsonAsync<JsonElement>("/api/v1/reference/antennas");
        publicList.EnumerateArray().Any(a => a.GetProperty("code").GetString() == code).Should().BeFalse();

        // Réactiver → réapparaît
        (await client.PostAsync($"/api/v1/antennas/{id}/activate", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        // Liste de gestion : inclut l'antenne (avec statut)
        var management = await client.GetFromJsonAsync<JsonElement>("/api/v1/antennas");
        management.EnumerateArray().Any(a => a.GetProperty("id").GetInt32() == id).Should().BeTrue();
    }

    [Fact]
    public async Task Deactivation_is_refused_when_an_open_session_exists()
    {
        var manager = ClientWith(_fixture.IssueReferentialsManagerToken());
        var code = UniqueCode();
        var created = await (await manager.PostAsJsonAsync("/api/v1/antennas", CreateBody(code))).Content.ReadFromJsonAsync<JsonElement>();
        var antennaId = created.GetProperty("id").GetInt32();

        // Ouvrir une session sur cette antenne (droit manage_attendance).
        var bureau = ClientWith(_fixture.IssueBureauToken());
        var session = await bureau.PostAsJsonAsync("/api/v1/attendance-sessions",
            new { antennaId, meetingDate = "2026-07-06", qrStepSeconds = 30 });
        session.StatusCode.Should().Be(HttpStatusCode.Created);

        // La désactivation est refusée tant que la session est ouverte.
        var response = await manager.PostAsync($"/api/v1/antennas/{antennaId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("antenna_has_open_sessions");
    }

    [Fact]
    public async Task Public_reference_read_stays_accessible_to_any_authenticated_user()
    {
        // La lecture publique (010) reste accessible sans manage_referentials (feature inchangée).
        var response = await ClientWith(_fixture.IssueMemberToken()).GetAsync("/api/v1/reference/antennas");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
