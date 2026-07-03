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
}
