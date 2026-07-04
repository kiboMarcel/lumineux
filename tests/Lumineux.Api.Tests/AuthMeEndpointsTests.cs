using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

/// <summary>
/// Tests d'intégration de <c>GET /api/v1/auth/me</c> (feature 007). US1 : identité + droits de la
/// session, sans secret, idempotent, droits strictement égaux à ceux du jeton. US2 : refus 401
/// uniforme sans session valide.
/// </summary>
public sealed class AuthMeEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public AuthMeEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    // --- US1 : lecture du profil de session ---

    [Fact]
    public async Task Me_with_token_returns_identity_and_effective_permissions()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMembersManagerToken());

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("memberId").GetInt32().Should().Be(43);
        doc.RootElement.GetProperty("displayName").GetString().Should().Be("bureau-membres");
        var permissions = doc.RootElement.GetProperty("permissions")
            .EnumerateArray().Select(e => e.GetString()).ToArray();
        // Égalité STRICTE avec les droits portés par le jeton (base des décisions d'autorisation).
        permissions.Should().BeEquivalentTo(new[] { "manage_members" });
    }

    [Fact]
    public async Task Me_response_contains_no_secret()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMembersManagerToken());

        var raw = await (await client.GetAsync("/api/v1/auth/me")).Content.ReadAsStringAsync();

        raw.Should().NotContainAny("passwordHash", "password", "token", "hash");
    }

    [Fact]
    public async Task Me_returns_empty_permissions_for_plain_member()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMemberToken());

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("permissions").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Me_is_idempotent_for_the_same_session()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMembersManagerToken());

        var first = await (await client.GetAsync("/api/v1/auth/me")).Content.ReadAsStringAsync();
        var second = await (await client.GetAsync("/api/v1/auth/me")).Content.ReadAsStringAsync();

        second.Should().Be(first); // lecture sans effet de bord (FR-008)
    }

    // --- US2 : refus de session ---

    [Fact]
    public async Task Me_without_token_returns_401()
    {
        var client = _fixture.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_invalid_token_returns_401()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "not-a-valid-jwt");

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
