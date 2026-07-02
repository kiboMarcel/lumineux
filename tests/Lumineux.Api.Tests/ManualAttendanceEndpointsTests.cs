using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

public sealed class ManualAttendanceEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public ManualAttendanceEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    private HttpClient BureauClient()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.IssueBureauToken());
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
    public async Task Add_list_and_cancel_manual_attendance_flow()
    {
        var bureau = BureauClient();
        var id = await CreateSessionAsync(bureau, "2026-12-01T09:00:00Z");
        var memberId = _fixture.SeededMemberId;

        var add = await bureau.PostAsJsonAsync($"/api/v1/attendance-sessions/{id}/attendances", new { memberId });
        add.StatusCode.Should().Be(HttpStatusCode.Created);
        using (var addDoc = JsonDocument.Parse(await add.Content.ReadAsStringAsync()))
        {
            addDoc.RootElement.GetProperty("source").GetString().Should().Be("Manual");
        }

        var list = await bureau.GetAsync($"/api/v1/attendance-sessions/{id}/attendances");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync()))
        {
            listDoc.RootElement.GetProperty("validCount").GetInt32().Should().Be(1);
        }

        var cancel = await bureau.DeleteAsync($"/api/v1/attendance-sessions/{id}/attendances/{memberId}");
        cancel.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterCancel = await bureau.GetAsync($"/api/v1/attendance-sessions/{id}/attendances");
        using var afterDoc = JsonDocument.Parse(await afterCancel.Content.ReadAsStringAsync());
        afterDoc.RootElement.GetProperty("validCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task AddManual_for_unknown_member_returns_404()
    {
        var bureau = BureauClient();
        var id = await CreateSessionAsync(bureau, "2026-12-02T09:00:00Z");

        var add = await bureau.PostAsJsonAsync(
            $"/api/v1/attendance-sessions/{id}/attendances", new { memberId = 999999 });

        add.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddManual_without_permission_returns_403()
    {
        var bureau = BureauClient();
        var id = await CreateSessionAsync(bureau, "2026-12-03T09:00:00Z");

        var member = _fixture.CreateClient();
        member.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMemberToken());

        var add = await member.PostAsJsonAsync(
            $"/api/v1/attendance-sessions/{id}/attendances", new { memberId = _fixture.SeededMemberId });

        add.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
