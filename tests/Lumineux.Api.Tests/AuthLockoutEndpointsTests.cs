using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

/// <summary>Verrouillage : après N échecs consécutifs, la connexion est refusée même avec le bon mot de passe (US4).</summary>
public sealed class AuthLockoutEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public AuthLockoutEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Login_locks_after_five_failed_attempts_and_rejects_valid_password()
    {
        await _fixture.SeedActiveMemberAccountAsync("LUM-LOCK-1", "Passw0rd");
        var client = _fixture.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var wrong = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { reference = "LUM-LOCK-1", password = "wrong-" + i });
            wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // Après le seuil : même le bon mot de passe est refusé (verrouillage temporaire).
        var lockedOut = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { reference = "LUM-LOCK-1", password = "Passw0rd" });
        lockedOut.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_success_resets_failure_counter()
    {
        await _fixture.SeedActiveMemberAccountAsync("LUM-LOCK-2", "Passw0rd");
        var client = _fixture.CreateClient();

        // 4 échecs (sous le seuil de 5).
        for (var i = 0; i < 4; i++)
        {
            var wrong = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { reference = "LUM-LOCK-2", password = "wrong-" + i });
            wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // Un succès remet le compteur à zéro.
        var success = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { reference = "LUM-LOCK-2", password = "Passw0rd" });
        success.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4 nouveaux échecs restent sous le seuil (car compteur remis à zéro) ; le bon mot de passe passe encore.
        for (var i = 0; i < 4; i++)
        {
            var wrong = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { reference = "LUM-LOCK-2", password = "wrong-again-" + i });
            wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var stillOk = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { reference = "LUM-LOCK-2", password = "Passw0rd" });
        stillOk.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
