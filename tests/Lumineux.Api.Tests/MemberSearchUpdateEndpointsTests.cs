using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

public sealed class MemberSearchUpdateEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public MemberSearchUpdateEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    private HttpClient ManagerClient()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMembersManagerToken());
        return client;
    }

    private async Task<int> CreateAsync(HttpClient client, string last, string first, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/members",
            new { lastName = last, firstName = first, gender = "F", email, antennaId = ApiTestFixture.SeededAntennaId });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("member").GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Search_get_and_update_flow()
    {
        var client = ManagerClient();
        var id = await CreateAsync(client, "Sanou", "Fatou", "fatou.sanou@example.com");

        var search = await client.GetAsync("/api/v1/members?query=Sanou");
        search.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await search.Content.ReadAsStringAsync()))
        {
            doc.RootElement.GetProperty("total").GetInt32().Should().BeGreaterThan(0);
            doc.RootElement.GetProperty("items").EnumerateArray()
                .Select(e => e.GetProperty("id").GetInt32()).Should().Contain(id);
        }

        var get = await client.GetAsync($"/api/v1/members/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var update = await client.PutAsJsonAsync($"/api/v1/members/{id}",
            new { lastName = "Sanou", firstName = "Fatou", gender = "F", email = "fatou.sanou@example.com", antennaId = ApiTestFixture.SeededAntennaId, address = "Cocody" });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await update.Content.ReadAsStringAsync()))
        {
            doc.RootElement.GetProperty("address").GetString().Should().Be("Cocody");
        }
    }

    [Fact]
    public async Task Get_unknown_member_returns_404()
    {
        var response = await ManagerClient().GetAsync("/api/v1/members/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_with_contact_of_another_active_member_returns_409()
    {
        var client = ManagerClient();
        await CreateAsync(client, "Bamba", "Rita", "rita.bamba@example.com");
        var secondId = await CreateAsync(client, "Cisse", "Nadia", "nadia.cisse@example.com");

        // Corriger le 2e membre avec l'e-mail du 1er → 409 contact_in_use
        var update = await client.PutAsJsonAsync($"/api/v1/members/{secondId}",
            new { lastName = "Cisse", firstName = "Nadia", gender = "F", email = "rita.bamba@example.com", antennaId = ApiTestFixture.SeededAntennaId });

        update.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var doc = JsonDocument.Parse(await update.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("contact_in_use");
    }

    [Fact]
    public async Task Search_without_permission_returns_403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMemberToken());

        var response = await client.GetAsync("/api/v1/members?query=x");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
