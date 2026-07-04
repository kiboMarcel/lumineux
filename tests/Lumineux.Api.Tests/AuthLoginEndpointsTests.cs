using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

public sealed class AuthLoginEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public AuthLoginEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Login_with_valid_credentials_returns_token()
    {
        await _fixture.SeedActiveMemberAccountAsync("LUM-LOGIN-1", "Passw0rd", "manage_attendance");
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { reference = "LUM-LOGIN-1", password = "Passw0rd" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("passwordHash");
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        doc.RootElement.GetProperty("tokenType").GetString().Should().Be("Bearer");
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        await _fixture.SeedActiveMemberAccountAsync("LUM-LOGIN-2", "Passw0rd");
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { reference = "LUM-LOGIN-2", password = "wrong" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_with_unknown_reference_returns_401()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { reference = "LUM-DOES-NOT-EXIST", password = "whatever" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_pending_account_returns_403_password_change_required()
    {
        await _fixture.SeedPendingMemberAccountAsync("LUM-LOGIN-3", "Temp1234");
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { reference = "LUM-LOGIN-3", password = "Temp1234" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("password_change_required");
    }
}
