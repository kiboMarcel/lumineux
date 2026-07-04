using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Lumineux.Application.Abstractions;
using Xunit;

namespace Lumineux.Api.Tests;

public sealed class AssignBureauProfileEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public AssignBureauProfileEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    private HttpClient AdminClient()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueBureauProfilesAdminToken());
        return client;
    }

    private async Task<int> CreateProfileAsync(HttpClient admin, string permission)
    {
        var name = "Assign-" + Guid.NewGuid().ToString("N")[..8];
        var response = await admin.PostAsJsonAsync("/api/v1/bureau-profiles", new
        {
            name, description = (string?)null, permissions = new[] { permission },
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Assign_valid_returns_204_and_login_token_carries_permission()
    {
        var admin = AdminClient();
        var profileId = await CreateProfileAsync(admin, Permissions.ManageAttendance);
        var memberId = await _fixture.SeedActiveMemberAccountAsync("ASSIGN-1", "Passw0rd");

        var assign = await admin.PostAsJsonAsync($"/api/v1/members/{memberId}/bureau-profiles",
            new { profileId });
        assign.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Reconnexion : le nouveau jeton doit permettre l'accès aux endpoints exigeant manage_attendance.
        var anonymous = _fixture.CreateClient();
        var login = await anonymous.PostAsJsonAsync("/api/v1/auth/login",
            new { reference = "ASSIGN-1", password = "Passw0rd" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var accessToken = JsonDocument.Parse(await login.Content.ReadAsStringAsync())
            .RootElement.GetProperty("accessToken").GetString();

        // Vérifier qu'il peut appeler un endpoint protégé par manage_attendance (démarrer une session).
        var member = _fixture.CreateClient();
        member.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var startSession = await member.PostAsJsonAsync("/api/v1/attendance-sessions", new
        {
            antennaId = ApiTestFixture.SeededAntennaId,
            step = 1,
            scheduledAt = DateTime.UtcNow.AddMinutes(5),
        });
        // 200/201 attendus si le droit est bien porté ; ce test se contente de vérifier qu'on N'A PAS 401/403.
        ((int)startSession.StatusCode).Should().NotBe((int)HttpStatusCode.Unauthorized);
        ((int)startSession.StatusCode).Should().NotBe((int)HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Assign_idempotent_returns_204_twice()
    {
        var admin = AdminClient();
        var profileId = await CreateProfileAsync(admin, Permissions.ManageMembers);
        var memberId = await _fixture.SeedActiveMemberAccountAsync("ASSIGN-2", "Passw0rd");

        var first = await admin.PostAsJsonAsync($"/api/v1/members/{memberId}/bureau-profiles", new { profileId });
        var second = await admin.PostAsJsonAsync($"/api/v1/members/{memberId}/bureau-profiles", new { profileId });

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Assign_unknown_profile_returns_404()
    {
        var admin = AdminClient();
        var memberId = await _fixture.SeedActiveMemberAccountAsync("ASSIGN-3", "Passw0rd");

        var response = await admin.PostAsJsonAsync($"/api/v1/members/{memberId}/bureau-profiles",
            new { profileId = 999999 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Assign_without_permission_returns_403()
    {
        var admin = AdminClient();
        var profileId = await CreateProfileAsync(admin, Permissions.ManageAttendance);
        var memberId = await _fixture.SeedActiveMemberAccountAsync("ASSIGN-4", "Passw0rd");

        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMemberToken());
        var response = await client.PostAsJsonAsync($"/api/v1/members/{memberId}/bureau-profiles",
            new { profileId });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Assign_without_token_returns_401()
    {
        var client = _fixture.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/members/1/bureau-profiles",
            new { profileId = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
