using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

/// <summary>
/// Tests d'intégration de <c>POST /api/v1/auth/reset-password</c> (US2, T022). Vérifient le succès
/// 204 (ancien mot de passe refusé / nouveau accepté), l'usage unique (rejeu 401), le refus générique
/// 401 d'un jeton inconnu, le rejet 400 d'un mot de passe faible (jeton réutilisable ensuite), et la
/// levée du verrouillage : un compte verrouillé peut se connecter immédiatement après reset (SC-007).
/// </summary>
public sealed class AuthResetPasswordEndpointsTests : IClassFixture<ApiTestFixture>
{
    private const string NewPassword = "NewPassw0rd";

    private readonly ApiTestFixture _fixture;

    public AuthResetPasswordEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Valid_token_resets_password_old_rejected_new_accepted()
    {
        var memberId = await _fixture.SeedMemberAccountAsync("LUM-RP-1", "OldPassw0rd", "rp1@example.org");
        var token = await _fixture.SeedResetTokenAsync(memberId);
        var client = _fixture.CreateClient();

        var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { token, newPassword = NewPassword });
        reset.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await Login(client, "LUM-RP-1", "OldPassw0rd")).Should().Be(HttpStatusCode.Unauthorized);
        (await Login(client, "LUM-RP-1", NewPassword)).Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Replayed_token_returns_401()
    {
        var memberId = await _fixture.SeedMemberAccountAsync("LUM-RP-2", "OldPassw0rd", "rp2@example.org");
        var token = await _fixture.SeedResetTokenAsync(memberId);
        var client = _fixture.CreateClient();

        var first = await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { token, newPassword = NewPassword });
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var replay = await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { token, newPassword = "AnotherPass1" });
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unknown_token_returns_401()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { token = "totally-unknown-token", newPassword = NewPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Weak_password_returns_400_then_valid_retry_succeeds()
    {
        var memberId = await _fixture.SeedMemberAccountAsync("LUM-RP-3", "OldPassw0rd", "rp3@example.org");
        var token = await _fixture.SeedResetTokenAsync(memberId);
        var client = _fixture.CreateClient();

        var weak = await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { token, newPassword = "weak" });
        weak.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Le jeton n'a pas été consommé : un réessai conforme réussit.
        var retry = await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { token, newPassword = NewPassword });
        retry.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Locked_account_can_login_immediately_after_reset()
    {
        var memberId = await _fixture.SeedMemberAccountAsync("LUM-RP-4", "OldPassw0rd", "rp4@example.org");
        var client = _fixture.CreateClient();

        // Verrouille le compte : 5 tentatives erronées (MaxFailedAttempts = 5).
        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/v1/auth/login",
                new { reference = "LUM-RP-4", password = "wrong" });
        }
        // Verrouillé : même le bon mot de passe est refusé.
        (await Login(client, "LUM-RP-4", "OldPassw0rd")).Should().Be(HttpStatusCode.Unauthorized);

        var token = await _fixture.SeedResetTokenAsync(memberId);
        var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { token, newPassword = NewPassword });
        reset.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Le verrouillage est levé : connexion immédiate avec le nouveau mot de passe (SC-007).
        (await Login(client, "LUM-RP-4", NewPassword)).Should().Be(HttpStatusCode.OK);
    }

    private static async Task<HttpStatusCode> Login(HttpClient client, string reference, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { reference, password });
        return response.StatusCode;
    }
}
