using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

public sealed class AuthActivateEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public AuthActivateEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Activate_then_login_flow()
    {
        await _fixture.SeedPendingMemberAccountAsync("LUM-ACT-1", "Temp1234");
        var client = _fixture.CreateClient();

        var activate = await client.PostAsJsonAsync("/api/v1/auth/activate",
            new { reference = "LUM-ACT-1", temporaryPassword = "Temp1234", newPassword = "NewPass1" });
        activate.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await activate.Content.ReadAsStringAsync()))
        {
            doc.RootElement.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        }

        // Le mot de passe temporaire ne permet plus la connexion ; le nouveau oui.
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { reference = "LUM-ACT-1", password = "NewPass1" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Activate_with_wrong_temporary_password_returns_401()
    {
        await _fixture.SeedPendingMemberAccountAsync("LUM-ACT-2", "Temp1234");
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/activate",
            new { reference = "LUM-ACT-2", temporaryPassword = "WRONG999", newPassword = "NewPass1" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Activate_with_weak_new_password_returns_400()
    {
        await _fixture.SeedPendingMemberAccountAsync("LUM-ACT-3", "Temp1234");
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/activate",
            new { reference = "LUM-ACT-3", temporaryPassword = "Temp1234", newPassword = "weak" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
