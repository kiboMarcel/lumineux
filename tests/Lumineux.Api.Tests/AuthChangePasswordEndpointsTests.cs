using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

public sealed class AuthChangePasswordEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public AuthChangePasswordEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ChangePassword_valid_returns_204_and_new_password_allows_login()
    {
        var memberId = await _fixture.SeedActiveMemberAccountAsync("LUM-CP-1", "Passw0rd");

        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMemberToken(memberId));

        var response = await client.PostAsJsonAsync("/api/v1/auth/change-password",
            new { currentPassword = "Passw0rd", newPassword = "NewPass1" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var anonymous = _fixture.CreateClient();
        var login = await anonymous.PostAsJsonAsync("/api/v1/auth/login",
            new { reference = "LUM-CP-1", password = "NewPass1" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_wrong_current_returns_401()
    {
        var memberId = await _fixture.SeedActiveMemberAccountAsync("LUM-CP-2", "Passw0rd");

        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMemberToken(memberId));

        var response = await client.PostAsJsonAsync("/api/v1/auth/change-password",
            new { currentPassword = "wrong", newPassword = "NewPass1" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_weak_new_returns_400()
    {
        var memberId = await _fixture.SeedActiveMemberAccountAsync("LUM-CP-3", "Passw0rd");

        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMemberToken(memberId));

        var response = await client.PostAsJsonAsync("/api/v1/auth/change-password",
            new { currentPassword = "Passw0rd", newPassword = "weak" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_without_token_returns_401()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/change-password",
            new { currentPassword = "Passw0rd", newPassword = "NewPass1" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
