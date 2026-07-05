using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

/// <summary>
/// Tests d'intégration de la recherche membre allégée (feature 015) : accès any-of, terme requis,
/// champs minimaux.
/// </summary>
public sealed class MemberLookupEndpointTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public MemberLookupEndpointTests(ApiTestFixture fixture) => _fixture = fixture;

    private HttpClient ClientWith(string token)
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Attendance_operator_gets_minimal_results()
    {
        // Le fixture amorce un membre « Doe / Jane » (M-FIXTURE-0001).
        var response = await ClientWith(_fixture.IssueBureauToken())
            .GetAsync("/api/v1/members/lookup?query=Doe");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContainAny("email", "mobile", "address", "birth");
        using var doc = JsonDocument.Parse(raw);
        var first = doc.RootElement.EnumerateArray().First();
        first.TryGetProperty("id", out _).Should().BeTrue();
        first.TryGetProperty("reference", out _).Should().BeTrue();
        first.GetProperty("fullName").GetString().Should().Contain("Doe");
    }

    [Fact]
    public async Task Without_attendance_or_members_permission_returns_403()
    {
        var response = await ClientWith(_fixture.IssueMemberToken())
            .GetAsync("/api/v1/members/lookup?query=Doe");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Without_token_returns_401()
    {
        var response = await _fixture.CreateClient().GetAsync("/api/v1/members/lookup?query=Doe");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Missing_query_returns_400()
    {
        var response = await ClientWith(_fixture.IssueBureauToken())
            .GetAsync("/api/v1/members/lookup");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
