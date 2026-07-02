using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

public sealed class AttendanceSessionsEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public AttendanceSessionsEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    private HttpClient CreateBureauClient()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueBureauToken());
        return client;
    }

    private static object NewSessionBody(string meetingDate) =>
        new { antennaId = ApiTestFixture.SeededAntennaId, meetingDate, qrStepSeconds = 30 };

    [Fact]
    public async Task StartSession_returns_201_and_never_exposes_secret()
    {
        var client = CreateBureauClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/attendance-sessions", NewSessionBody("2026-07-05T09:00:00Z"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("qrSecret").And.NotContain("secret");

        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Open");
        doc.RootElement.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task StartSession_without_token_returns_401()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/attendance-sessions", NewSessionBody("2026-08-01T09:00:00Z"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StartSession_without_permission_returns_403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMemberToken());

        var response = await client.PostAsJsonAsync(
            "/api/v1/attendance-sessions", NewSessionBody("2026-08-02T09:00:00Z"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task StartSession_duplicate_open_session_returns_409()
    {
        var client = CreateBureauClient();
        var body = NewSessionBody("2026-09-10T09:00:00Z");

        var first = await client.PostAsJsonAsync("/api/v1/attendance-sessions", body);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/v1/attendance-sessions", body);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetQr_returns_rotating_token()
    {
        var client = CreateBureauClient();

        var create = await client.PostAsJsonAsync(
            "/api/v1/attendance-sessions", NewSessionBody("2026-10-15T09:00:00Z"));
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetInt32();

        var qr = await client.GetAsync($"/api/v1/attendance-sessions/{id}/qr");
        qr.StatusCode.Should().Be(HttpStatusCode.OK);

        using var qrDoc = JsonDocument.Parse(await qr.Content.ReadAsStringAsync());
        qrDoc.RootElement.GetProperty("token").GetString().Should().MatchRegex("^[0-9]{8}$");
        qrDoc.RootElement.GetProperty("stepSeconds").GetInt32().Should().Be(30);
    }
}
