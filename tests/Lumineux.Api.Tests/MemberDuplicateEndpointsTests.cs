using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

public sealed class MemberDuplicateEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public MemberDuplicateEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    private HttpClient ManagerClient()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMembersManagerToken());
        return client;
    }

    [Fact]
    public async Task Homonym_is_flagged_then_created_after_confirmation()
    {
        var client = ManagerClient();

        var first = await client.PostAsJsonAsync("/api/v1/members",
            new { lastName = "Kone", firstName = "Awa", gender = "F", email = "awa.kone.1@example.com", antennaId = ApiTestFixture.SeededAntennaId });
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        using var firstDoc = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        var firstId = firstDoc.RootElement.GetProperty("member").GetProperty("id").GetInt32();

        // Même nom+prénom, contact différent, sans confirmation → 409 duplicate_name
        var conflict = await client.PostAsJsonAsync("/api/v1/members",
            new { lastName = "Kone", firstName = "Awa", gender = "F", email = "awa.kone.2@example.com", antennaId = ApiTestFixture.SeededAntennaId });
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using (var doc = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync()))
        {
            doc.RootElement.GetProperty("code").GetString().Should().Be("duplicate_name");
            doc.RootElement.GetProperty("duplicateMemberIds").EnumerateArray()
                .Select(e => e.GetInt32()).Should().Contain(firstId);
        }

        // Avec confirmation → 201
        var confirmed = await client.PostAsJsonAsync("/api/v1/members",
            new { lastName = "Kone", firstName = "Awa", gender = "F", email = "awa.kone.2@example.com", antennaId = ApiTestFixture.SeededAntennaId, confirmDuplicate = true });
        confirmed.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Contact_already_used_by_active_member_returns_409()
    {
        var client = ManagerClient();

        var created = await client.PostAsJsonAsync("/api/v1/members",
            new { lastName = "Diallo", firstName = "Ibrahim", gender = "M", email = "ibrahim.diallo@example.com", antennaId = ApiTestFixture.SeededAntennaId });
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        // Autre personne, même e-mail → 409 contact_in_use
        var conflict = await client.PostAsJsonAsync("/api/v1/members",
            new { lastName = "Autre", firstName = "Personne", gender = "F", email = "ibrahim.diallo@example.com", antennaId = ApiTestFixture.SeededAntennaId });
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var doc = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("contact_in_use");
    }
}
