using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

/// <summary>
/// Tests d'intégration de <c>POST /api/v1/auth/forgot-password</c> (US1, T015). Vérifient la réponse
/// générique 200 et surtout son <b>égalité octet à octet</b> entre un compte éligible, une référence
/// inexistante, un compte sans email et un compte archivé (anti-énumération, SC-002).
/// </summary>
public sealed class AuthForgotPasswordEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public AuthForgotPasswordEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Eligible_account_gets_generic_200_and_email_is_sent()
    {
        await _fixture.SeedMemberAccountAsync("LUM-FP-1", "Passw0rd", "fp1@example.org");
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new { reference = "LUM-FP-1" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _fixture.Email.ResetLinkFor("fp1@example.org").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Response_is_byte_for_byte_identical_across_all_cases()
    {
        await _fixture.SeedMemberAccountAsync("LUM-FP-ACTIVE", "Passw0rd", "fp-active@example.org");
        await _fixture.SeedMemberAccountAsync("LUM-FP-NOEMAIL", "Passw0rd", email: null);
        await _fixture.SeedMemberAccountAsync("LUM-FP-ARCHIVED", "Passw0rd", "fp-arch@example.org", status: "Archived");
        var client = _fixture.CreateClient();

        var active = await Post(client, "LUM-FP-ACTIVE");
        var unknown = await Post(client, "LUM-FP-DOES-NOT-EXIST");
        var noEmail = await Post(client, "LUM-FP-NOEMAIL");
        var archived = await Post(client, "LUM-FP-ARCHIVED");

        unknown.Should().Be(active);
        noEmail.Should().Be(active);
        archived.Should().Be(active);
    }

    [Fact]
    public async Task Empty_reference_returns_400()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new { reference = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<string> Post(HttpClient client, string reference)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { reference });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync();
    }
}
