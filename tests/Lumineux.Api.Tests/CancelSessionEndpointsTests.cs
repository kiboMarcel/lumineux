using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

public sealed class CancelSessionEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public CancelSessionEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

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

    private async Task<int> CreateSessionAsync(HttpClient bureau, string meetingDate)
    {
        var create = await bureau.PostAsJsonAsync(
            "/api/v1/attendance-sessions",
            new { antennaId = ApiTestFixture.SeededAntennaId, meetingDate, qrStepSeconds = 30 });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        using var doc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Cancel_empty_open_session_returns_200_cancelled()
    {
        var bureau = BureauClient();
        var id = await CreateSessionAsync(bureau, "2027-02-10T09:00:00Z");

        var cancel = await bureau.PostAsync($"/api/v1/attendance-sessions/{id}/cancel", null);

        cancel.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await cancel.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("status").GetString().Should().Be("Cancelled");
    }

    [Fact]
    public async Task Cancel_session_with_presence_returns_409()
    {
        var bureau = BureauClient();
        var id = await CreateSessionAsync(bureau, "2027-02-11T09:00:00Z");

        var add = await bureau.PostAsJsonAsync(
            $"/api/v1/attendance-sessions/{id}/attendances", new { memberId = _fixture.SeededMemberId });
        add.StatusCode.Should().Be(HttpStatusCode.Created);

        var cancel = await bureau.PostAsync($"/api/v1/attendance-sessions/{id}/cancel", null);

        cancel.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cancel_closed_session_returns_409()
    {
        var bureau = BureauClient();
        var id = await CreateSessionAsync(bureau, "2027-02-12T09:00:00Z");

        var close = await bureau.PostAsync($"/api/v1/attendance-sessions/{id}/close", null);
        close.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancel = await bureau.PostAsync($"/api/v1/attendance-sessions/{id}/cancel", null);

        cancel.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cancel_unknown_session_returns_404()
    {
        var cancel = await BureauClient().PostAsync("/api/v1/attendance-sessions/999999/cancel", null);

        cancel.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_without_manage_attendance_returns_403()
    {
        var bureau = BureauClient();
        var id = await CreateSessionAsync(bureau, "2027-02-13T09:00:00Z");

        var cancel = await MemberClient().PostAsync($"/api/v1/attendance-sessions/{id}/cancel", null);

        cancel.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
