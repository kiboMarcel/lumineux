using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

public sealed class ScanEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public ScanEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    private HttpClient BureauClient()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.IssueBureauToken());
        return client;
    }

    private HttpClient MemberClient()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.IssueMemberToken());
        return client;
    }

    private async Task<(int Id, string Token)> CreateSessionWithTokenAsync(string meetingDate)
    {
        var bureau = BureauClient();
        var create = await bureau.PostAsJsonAsync(
            "/api/v1/attendance-sessions",
            new { antennaId = ApiTestFixture.SeededAntennaId, meetingDate, qrStepSeconds = 30 });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        using var createdDoc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = createdDoc.RootElement.GetProperty("id").GetInt32();

        var qr = await bureau.GetAsync($"/api/v1/attendance-sessions/{id}/qr");
        qr.StatusCode.Should().Be(HttpStatusCode.OK);
        using var qrDoc = JsonDocument.Parse(await qr.Content.ReadAsStringAsync());
        var token = qrDoc.RootElement.GetProperty("token").GetString()!;

        return (id, token);
    }

    [Fact]
    public async Task Scan_records_presence_then_returns_already_present_on_rescan()
    {
        var (id, token) = await CreateSessionWithTokenAsync("2026-11-01T09:00:00Z");
        var member = MemberClient();

        var first = await member.PostAsJsonAsync($"/api/v1/attendance-sessions/{id}/scan", new { token });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        using var body = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("source").GetString().Should().Be("QrScan");

        var second = await member.PostAsJsonAsync($"/api/v1/attendance-sessions/{id}/scan", new { token });
        second.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Scan_with_invalid_token_returns_410()
    {
        var (id, _) = await CreateSessionWithTokenAsync("2026-11-02T09:00:00Z");
        var member = MemberClient();

        var response = await member.PostAsJsonAsync(
            $"/api/v1/attendance-sessions/{id}/scan", new { token = "invalid-token" });

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task Scan_without_token_returns_401()
    {
        var (id, token) = await CreateSessionWithTokenAsync("2026-11-03T09:00:00Z");
        var anonymous = _fixture.CreateClient();

        var response = await anonymous.PostAsJsonAsync(
            $"/api/v1/attendance-sessions/{id}/scan", new { token });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Batch_sync_creates_then_is_idempotent()
    {
        var (id, token) = await CreateSessionWithTokenAsync("2026-11-04T09:00:00Z");
        var member = MemberClient();
        var arrival = DateTime.UtcNow.ToString("o");
        var batch = new { items = new[] { new { clientOperationId = "op-batch-1", token, clientArrivalTime = arrival } } };

        var first = await member.PostAsJsonAsync($"/api/v1/attendance-sessions/{id}/scan/batch", batch);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        using var firstDoc = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        firstDoc.RootElement.GetProperty("results")[0].GetProperty("outcome").GetString().Should().Be("Created");

        var second = await member.PostAsJsonAsync($"/api/v1/attendance-sessions/{id}/scan/batch", batch);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        using var secondDoc = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        secondDoc.RootElement.GetProperty("results")[0].GetProperty("outcome").GetString().Should().Be("AlreadyPresent");
    }
}
