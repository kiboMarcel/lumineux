using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

/// <summary>Tests d'intégration des rapports de présence (feature 018) : structure, RBAC, validation.</summary>
public sealed class ReportsEndpointsTests : IClassFixture<ApiTestFixture>
{
    private const string Range = "from=2026-06-01&to=2026-06-30";
    private readonly ApiTestFixture _fixture;

    public ReportsEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    private HttpClient ClientWith(string token)
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Antenna_summary_returns_200_for_manager()
    {
        var response = await ClientWith(_fixture.IssueBureauToken())
            .GetAsync($"/api/v1/reports/attendance/antenna-summary?{Range}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.TryGetProperty("items", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Invalid_period_returns_400()
    {
        var response = await ClientWith(_fixture.IssueBureauToken())
            .GetAsync("/api/v1/reports/attendance/antenna-summary?from=2026-06-30&to=2026-06-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Without_token_returns_401()
    {
        var response = await _fixture.CreateClient()
            .GetAsync($"/api/v1/reports/attendance/antenna-summary?{Range}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Without_manage_attendance_returns_403()
    {
        var response = await ClientWith(_fixture.IssueMemberToken())
            .GetAsync($"/api/v1/reports/attendance/antenna-summary?{Range}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Csv_export_returns_text_csv()
    {
        var response = await ClientWith(_fixture.IssueBureauToken())
            .GetAsync($"/api/v1/reports/attendance/antenna-summary.csv?{Range}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var text = await response.Content.ReadAsStringAsync();
        text.Should().Contain("Antenne;Sessions;Présences valides;Moyenne par séance");
    }

    [Fact]
    public async Task Member_rate_returns_200_for_seeded_member_without_division_error()
    {
        var response = await ClientWith(_fixture.IssueBureauToken())
            .GetAsync($"/api/v1/reports/attendance/member-rate?memberId={_fixture.SeededMemberId}&{Range}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("rate").GetDecimal().Should().Be(0m); // aucune présence → 0, pas d'erreur
    }

    [Fact]
    public async Task Member_rate_unknown_member_returns_404()
    {
        var response = await ClientWith(_fixture.IssueBureauToken())
            .GetAsync($"/api/v1/reports/attendance/member-rate?memberId=999999&{Range}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Time_series_returns_200_with_continuous_points()
    {
        var response = await ClientWith(_fixture.IssueBureauToken())
            .GetAsync($"/api/v1/reports/attendance/time-series?{Range}&granularity=Month");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("points").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Time_series_unsupported_granularity_returns_400()
    {
        var response = await ClientWith(_fixture.IssueBureauToken())
            .GetAsync($"/api/v1/reports/attendance/time-series?{Range}&granularity=Day");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Time_series_without_manage_attendance_returns_403()
    {
        var response = await ClientWith(_fixture.IssueMemberToken())
            .GetAsync($"/api/v1/reports/attendance/time-series?{Range}&granularity=Month");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
