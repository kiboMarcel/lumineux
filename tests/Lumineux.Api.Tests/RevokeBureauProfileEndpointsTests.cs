using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Lumineux.Application.Abstractions;
using Xunit;

namespace Lumineux.Api.Tests;

public sealed class RevokeBureauProfileEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public RevokeBureauProfileEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    private HttpClient AdminClient()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueBureauProfilesAdminToken());
        return client;
    }

    private async Task<int> CreateProfileAsync(HttpClient admin, string permission)
    {
        var response = await admin.PostAsJsonAsync("/api/v1/bureau-profiles", new
        {
            name = "Rev-" + Guid.NewGuid().ToString("N")[..8],
            description = (string?)null,
            permissions = new[] { permission },
        });
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Revoke_valid_returns_204_and_next_token_lacks_permission()
    {
        var admin = AdminClient();
        var profileId = await CreateProfileAsync(admin, Permissions.ManageAttendance);
        var memberId = await _fixture.SeedActiveMemberAccountAsync("REV-1", "Passw0rd");

        // Attribuer puis révoquer
        var assign = await admin.PostAsJsonAsync($"/api/v1/members/{memberId}/bureau-profiles", new { profileId });
        assign.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var revoke = await admin.DeleteAsync($"/api/v1/members/{memberId}/bureau-profiles/{profileId}");
        revoke.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Reconnexion : le nouveau jeton n'a plus manage_attendance → l'endpoint protégé refuse (403).
        var anonymous = _fixture.CreateClient();
        var login = await anonymous.PostAsJsonAsync("/api/v1/auth/login",
            new { reference = "REV-1", password = "Passw0rd" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var accessToken = JsonDocument.Parse(await login.Content.ReadAsStringAsync())
            .RootElement.GetProperty("accessToken").GetString();

        var member = _fixture.CreateClient();
        member.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var startSession = await member.PostAsJsonAsync("/api/v1/attendance-sessions", new
        {
            antennaId = ApiTestFixture.SeededAntennaId,
            step = 1,
            scheduledAt = DateTime.UtcNow.AddMinutes(5),
        });
        startSession.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Revoke_missing_assignment_returns_404()
    {
        var admin = AdminClient();
        var profileId = await CreateProfileAsync(admin, Permissions.ManageAttendance);
        var memberId = await _fixture.SeedActiveMemberAccountAsync("REV-2", "Passw0rd");

        var response = await admin.DeleteAsync($"/api/v1/members/{memberId}/bureau-profiles/{profileId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Revoke_without_permission_returns_403()
    {
        var admin = AdminClient();
        var profileId = await CreateProfileAsync(admin, Permissions.ManageAttendance);
        var memberId = await _fixture.SeedActiveMemberAccountAsync("REV-3", "Passw0rd");
        await admin.PostAsJsonAsync($"/api/v1/members/{memberId}/bureau-profiles", new { profileId });

        var noRights = _fixture.CreateClient();
        noRights.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMemberToken());
        var response = await noRights.DeleteAsync($"/api/v1/members/{memberId}/bureau-profiles/{profileId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
