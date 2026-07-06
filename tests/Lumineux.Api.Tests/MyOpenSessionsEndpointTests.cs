using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

/// <summary>Tests d'intégration de « mes sessions ouvertes » (feature 023) : reprise, RBAC, isolation.</summary>
public sealed class MyOpenSessionsEndpointTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public MyOpenSessionsEndpointTests(ApiTestFixture fixture) => _fixture = fixture;

    private HttpClient ClientWith(string token)
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Returns_my_open_session_after_start()
    {
        var bureau = ClientWith(_fixture.IssueBureauToken());

        // Démarrer une session (créneau unique pour éviter tout conflit avec d'autres tests).
        var start = await bureau.PostAsJsonAsync("/api/v1/attendance-sessions",
            new { antennaId = ApiTestFixture.SeededAntennaId, meetingDate = "2027-03-03", qrStepSeconds = 30 });
        start.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await start.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var mine = await bureau.GetAsync("/api/v1/attendance-sessions/mine/open");

        mine.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await mine.Content.ReadFromJsonAsync<JsonElement>();
        list.EnumerateArray().Any(s => s.GetProperty("id").GetInt32() == id).Should().BeTrue();
    }

    [Fact]
    public async Task Without_token_returns_401()
    {
        var response = await _fixture.CreateClient().GetAsync("/api/v1/attendance-sessions/mine/open");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Without_manage_attendance_returns_403()
    {
        var response = await ClientWith(_fixture.IssueMemberToken())
            .GetAsync("/api/v1/attendance-sessions/mine/open");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
