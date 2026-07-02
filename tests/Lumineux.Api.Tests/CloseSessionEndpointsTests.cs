using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

public sealed class CloseSessionEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public CloseSessionEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

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
    public async Task Close_sets_end_time_and_propagates_to_attendances()
    {
        var bureau = BureauClient();
        var id = await CreateSessionAsync(bureau, "2027-01-10T09:00:00Z");

        var add = await bureau.PostAsJsonAsync(
            $"/api/v1/attendance-sessions/{id}/attendances", new { memberId = _fixture.SeededMemberId });
        add.StatusCode.Should().Be(HttpStatusCode.Created);

        var close = await bureau.PostAsync($"/api/v1/attendance-sessions/{id}/close", null);
        close.StatusCode.Should().Be(HttpStatusCode.OK);

        string endTime;
        using (var closeDoc = JsonDocument.Parse(await close.Content.ReadAsStringAsync()))
        {
            closeDoc.RootElement.GetProperty("status").GetString().Should().Be("Closed");
            endTime = closeDoc.RootElement.GetProperty("endTime").GetString()!;
            endTime.Should().NotBeNullOrEmpty();
        }

        var list = await bureau.GetAsync($"/api/v1/attendance-sessions/{id}/attendances?status=All");
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        foreach (var item in listDoc.RootElement.GetProperty("items").EnumerateArray())
        {
            item.GetProperty("endTime").GetString().Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task Operations_after_close_are_rejected_with_409()
    {
        var bureau = BureauClient();
        var id = await CreateSessionAsync(bureau, "2027-01-11T09:00:00Z");

        var close = await bureau.PostAsync($"/api/v1/attendance-sessions/{id}/close", null);
        close.StatusCode.Should().Be(HttpStatusCode.OK);

        var scan = await MemberClient().PostAsJsonAsync(
            $"/api/v1/attendance-sessions/{id}/scan", new { token = "12345678" });
        scan.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var manual = await bureau.PostAsJsonAsync(
            $"/api/v1/attendance-sessions/{id}/attendances", new { memberId = _fixture.SeededMemberId });
        manual.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var secondClose = await bureau.PostAsync($"/api/v1/attendance-sessions/{id}/close", null);
        secondClose.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
