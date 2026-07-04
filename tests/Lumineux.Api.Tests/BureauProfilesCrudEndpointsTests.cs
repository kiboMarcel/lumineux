using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Lumineux.Application.Abstractions;
using Xunit;

namespace Lumineux.Api.Tests;

public sealed class BureauProfilesCrudEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public BureauProfilesCrudEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    private HttpClient AdminClient()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueBureauProfilesAdminToken());
        return client;
    }

    [Fact]
    public async Task Create_valid_profile_returns_201()
    {
        var client = AdminClient();
        var name = "Test-Crud-Create-" + Guid.NewGuid().ToString("N")[..8];

        var response = await client.PostAsJsonAsync("/api/v1/bureau-profiles", new
        {
            name,
            description = "Description",
            permissions = new[] { Permissions.ManageAttendance },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("name").GetString().Should().Be(name);
    }

    [Fact]
    public async Task Create_duplicate_name_returns_409_duplicate_name()
    {
        var client = AdminClient();
        var name = "Test-Crud-Dup-" + Guid.NewGuid().ToString("N")[..8];

        var first = await client.PostAsJsonAsync("/api/v1/bureau-profiles", new
        {
            name, description = (string?)null, permissions = new[] { Permissions.ManageAttendance },
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await client.PostAsJsonAsync("/api/v1/bureau-profiles", new
        {
            name = name.ToUpperInvariant(),
            description = (string?)null,
            permissions = new[] { Permissions.ManageMembers },
        });

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var doc = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("duplicate_name");
    }

    [Fact]
    public async Task Create_unknown_permission_returns_400()
    {
        var client = AdminClient();

        var response = await client.PostAsJsonAsync("/api/v1/bureau-profiles", new
        {
            name = "Test-Crud-BadPerm-" + Guid.NewGuid().ToString("N")[..8],
            description = (string?)null,
            permissions = new[] { "unknown_right" },
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_without_admin_permission_returns_403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMemberToken());

        var response = await client.PostAsJsonAsync("/api/v1/bureau-profiles", new
        {
            name = "Test", description = (string?)null, permissions = new[] { Permissions.ManageAttendance },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_valid_returns_200_with_new_state()
    {
        var client = AdminClient();
        var name = "Test-Crud-Upd-" + Guid.NewGuid().ToString("N")[..8];
        var create = await client.PostAsJsonAsync("/api/v1/bureau-profiles", new
        {
            name, description = "old", permissions = new[] { Permissions.ManageAttendance },
        });
        var id = JsonDocument.Parse(await create.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var response = await client.PutAsJsonAsync($"/api/v1/bureau-profiles/{id}", new
        {
            name, description = "new", permissions = new[] { Permissions.ManageMembers },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("description").GetString().Should().Be("new");
        doc.RootElement.GetProperty("permissions").EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo(new[] { Permissions.ManageMembers });
    }

    [Fact]
    public async Task Delete_unassigned_profile_returns_204()
    {
        var client = AdminClient();
        var name = "Test-Crud-Del-" + Guid.NewGuid().ToString("N")[..8];
        var create = await client.PostAsJsonAsync("/api/v1/bureau-profiles", new
        {
            name, description = (string?)null, permissions = new[] { Permissions.ManageAttendance },
        });
        var id = JsonDocument.Parse(await create.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt32();

        var response = await client.DeleteAsync($"/api/v1/bureau-profiles/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_unknown_profile_returns_404()
    {
        var client = AdminClient();

        var response = await client.DeleteAsync("/api/v1/bureau-profiles/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
