using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Lumineux.Application.Abstractions;
using Xunit;

namespace Lumineux.Api.Tests;

public sealed class BureauProfilesQueryEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public BureauProfilesQueryEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    private HttpClient AdminClient()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueBureauProfilesAdminToken());
        return client;
    }

    private HttpClient MembersManagerClient()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMembersManagerToken());
        return client;
    }

    [Fact]
    public async Task List_permissions_returns_exact_catalog()
    {
        var response = await AdminClient().GetAsync("/api/v1/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var codes = doc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("code").GetString())
            .ToArray();
        codes.Should().BeEquivalentTo(new[]
        {
            Permissions.ManageAttendance, Permissions.ManageMembers, Permissions.ManageBureauProfiles,
        });
    }

    [Fact]
    public async Task List_permissions_without_token_returns_401()
    {
        var response = await _fixture.CreateClient().GetAsync("/api/v1/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_profiles_with_admin_returns_200()
    {
        var admin = AdminClient();
        var name = "QL-" + Guid.NewGuid().ToString("N")[..8];
        await admin.PostAsJsonAsync("/api/v1/bureau-profiles", new
        {
            name, description = "d", permissions = new[] { Permissions.ManageAttendance },
        });

        var response = await admin.GetAsync("/api/v1/bureau-profiles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().Contain(name);
        // FR-016 : aucune donnée sensible dans les réponses.
        raw.Should().NotContain("passwordHash");
    }

    [Fact]
    public async Task List_profiles_with_manage_members_returns_200()
    {
        var response = await MembersManagerClient().GetAsync("/api/v1/bureau-profiles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task List_profiles_without_read_permission_returns_403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMemberToken());

        var response = await client.GetAsync("/api/v1/bureau-profiles");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_profile_detail_returns_members()
    {
        var admin = AdminClient();
        var create = await admin.PostAsJsonAsync("/api/v1/bureau-profiles", new
        {
            name = "QGet-" + Guid.NewGuid().ToString("N")[..8],
            description = (string?)null,
            permissions = new[] { Permissions.ManageAttendance },
        });
        var profileId = JsonDocument.Parse(await create.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetInt32();
        var memberId = await _fixture.SeedActiveMemberAccountAsync("QGET-" + Guid.NewGuid().ToString("N")[..8], "P");
        await admin.PostAsJsonAsync($"/api/v1/members/{memberId}/bureau-profiles", new { profileId });

        var response = await admin.GetAsync($"/api/v1/bureau-profiles/{profileId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("passwordHash").And.NotContain("mobile").And.NotContain("email");
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("members").GetArrayLength().Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task Get_profile_unknown_returns_404()
    {
        var response = await AdminClient().GetAsync("/api/v1/bureau-profiles/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_member_profiles_returns_effective_permissions()
    {
        var admin = AdminClient();
        var p1 = await admin.PostAsJsonAsync("/api/v1/bureau-profiles", new
        {
            name = "QMem-A-" + Guid.NewGuid().ToString("N")[..8],
            description = (string?)null,
            permissions = new[] { Permissions.ManageAttendance },
        });
        var p1Id = JsonDocument.Parse(await p1.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();
        var p2 = await admin.PostAsJsonAsync("/api/v1/bureau-profiles", new
        {
            name = "QMem-B-" + Guid.NewGuid().ToString("N")[..8],
            description = (string?)null,
            permissions = new[] { Permissions.ManageMembers, Permissions.ManageAttendance },
        });
        var p2Id = JsonDocument.Parse(await p2.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var memberId = await _fixture.SeedActiveMemberAccountAsync("QMEM-" + Guid.NewGuid().ToString("N")[..8], "P");
        await admin.PostAsJsonAsync($"/api/v1/members/{memberId}/bureau-profiles", new { profileId = p1Id });
        await admin.PostAsJsonAsync($"/api/v1/members/{memberId}/bureau-profiles", new { profileId = p2Id });

        var response = await admin.GetAsync($"/api/v1/members/{memberId}/bureau-profiles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var effective = doc.RootElement.GetProperty("effectivePermissions").EnumerateArray()
            .Select(e => e.GetString()).ToArray();
        effective.Should().BeEquivalentTo(new[] { Permissions.ManageAttendance, Permissions.ManageMembers });
        // FR-016 : la vue publique du membre n'expose pas de secret.
        var raw = doc.RootElement.GetRawText();
        raw.Should().NotContain("passwordHash");
    }
}
