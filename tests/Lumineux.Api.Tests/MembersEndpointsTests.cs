using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Xunit;

namespace Lumineux.Api.Tests;

public sealed class MembersEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public MembersEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    private HttpClient ManagerClient()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMembersManagerToken());
        return client;
    }

    [Fact]
    public async Task Create_with_email_returns_201_email_sent_without_secret()
    {
        var client = ManagerClient();
        var body = new
        {
            lastName = "Martin", firstName = "Claire", gender = "F",
            email = "claire.martin.a@example.com", antennaId = ApiTestFixture.SeededAntennaId,
        };

        var response = await client.PostAsJsonAsync("/api/v1/members", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("passwordHash").And.NotContain("Hash");

        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("credentialsDelivery").GetString().Should().Be("EmailSent");
        doc.RootElement.GetProperty("temporaryPassword").ValueKind.Should().Be(JsonValueKind.Null);
        doc.RootElement.GetProperty("member").GetProperty("reference").GetString().Should().NotBeNullOrEmpty();
        doc.RootElement.GetProperty("member").GetProperty("status").GetString().Should().Be("Active");
    }

    [Fact]
    public async Task Create_without_email_returns_bureau_handout_with_temp_password()
    {
        var client = ManagerClient();
        var body = new
        {
            lastName = "Traore", firstName = "Ali", gender = "M",
            mobile = "+2250700000001", antennaId = ApiTestFixture.SeededAntennaId,
        };

        var response = await client.PostAsJsonAsync("/api/v1/members", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("credentialsDelivery").GetString().Should().Be("BureauHandout");
        doc.RootElement.GetProperty("temporaryPassword").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Create_without_token_returns_401()
    {
        var client = _fixture.CreateClient();
        var body = new { lastName = "X", firstName = "Y", gender = "F", email = "x.y@example.com", antennaId = 1 };

        var response = await client.PostAsJsonAsync("/api/v1/members", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_without_manage_members_permission_returns_403()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMemberToken()); // membre standard

        var body = new { lastName = "X", firstName = "Y", gender = "F", email = "x.z@example.com", antennaId = 1 };
        var response = await client.PostAsJsonAsync("/api/v1/members", body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_with_unknown_antenna_returns_404()
    {
        var client = ManagerClient();
        var body = new { lastName = "No", firstName = "Antenna", gender = "M", email = "no.antenna@example.com", antennaId = 999999 };

        var response = await client.PostAsJsonAsync("/api/v1/members", body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_without_contact_returns_400()
    {
        var client = ManagerClient();
        var body = new { lastName = "No", firstName = "Contact", gender = "F", antennaId = ApiTestFixture.SeededAntennaId };

        var response = await client.PostAsJsonAsync("/api/v1/members", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- Feature 030 : profession ----

    [Fact]
    public async Task Create_with_profession_returns_it()
    {
        var client = ManagerClient();
        var body = new
        {
            lastName = "Kone", firstName = "Awa", gender = "F",
            email = "awa.kone.prof@example.com", antennaId = ApiTestFixture.SeededAntennaId,
            profession = "Enseignante",
        };

        var response = await client.PostAsJsonAsync("/api/v1/members", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("member").GetProperty("profession").GetString().Should().Be("Enseignante");
    }

    [Fact]
    public async Task Create_without_profession_returns_null_profession()
    {
        // Régression SC-004 : un membre sans profession se lit avec profession null (via GET).
        var client = ManagerClient();
        var body = new
        {
            lastName = "Yao", firstName = "Kofi", gender = "M",
            email = "kofi.yao.noprof@example.com", antennaId = ApiTestFixture.SeededAntennaId,
        };

        var create = await client.PostAsJsonAsync("/api/v1/members", body);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        created.RootElement.GetProperty("member").GetProperty("profession").ValueKind.Should().Be(JsonValueKind.Null);
        var id = created.RootElement.GetProperty("member").GetProperty("id").GetInt32();

        var get = await client.GetAsync($"/api/v1/members/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        using var read = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        read.RootElement.GetProperty("profession").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Two_members_can_share_the_same_profession()
    {
        // FR-010 : aucune contrainte d'unicité sur la profession.
        var client = ManagerClient();
        var first = new
        {
            lastName = "Diallo", firstName = "Sara", gender = "F",
            email = "sara.diallo.job@example.com", antennaId = ApiTestFixture.SeededAntennaId, profession = "Commerçant",
        };
        var second = new
        {
            lastName = "Bah", firstName = "Moussa", gender = "M",
            email = "moussa.bah.job@example.com", antennaId = ApiTestFixture.SeededAntennaId, profession = "Commerçant",
        };

        (await client.PostAsJsonAsync("/api/v1/members", first)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/api/v1/members", second)).StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
