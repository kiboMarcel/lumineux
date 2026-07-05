using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Lumineux.Domain.Entities;
using Lumineux.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lumineux.Api.Tests;

/// <summary>
/// Tests d'intégration des endpoints de données de référence (feature 010) : listes actives et
/// triées avec jeton, exclusion des entrées inactives, refus 401 sans authentification.
/// </summary>
public sealed class ReferenceEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public ReferenceEndpointsTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        Seed();
    }

    private void Seed()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!db.Antennas.Any(a => a.Code == "ZINACT"))
        {
            db.Antennas.Add(new Antenna { Code = "ZINACT", Label = "ZZZ Antenne inactive", District = 1, Status = "Archived" });
        }
        if (!db.Civilities.Any())
        {
            db.Civilities.Add(new Civility { Code = "MME", Label = "Madame", Status = "Active" });
            db.Cities.Add(new City { Code = "ABJ", Label = "Abidjan", Status = "Active" });
            db.Districts.Add(new District { Code = "COC", Label = "Cocody", Status = "Active" });
            db.Countries.Add(new Country { Code = "CI", LabelCountry = "Côte d'Ivoire", LabelNationality = "Ivoirienne", Status = "Active" });
            db.Countries.Add(new Country { Code = "XX", LabelCountry = "Pays inactif", LabelNationality = "Inactive", Status = "Archived" });
        }
        db.SaveChanges();
    }

    private HttpClient AuthenticatedClient()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _fixture.IssueMemberToken());
        return client;
    }

    private static List<JsonElement> ParseArray(string raw) =>
        JsonDocument.Parse(raw).RootElement.EnumerateArray().ToList();

    [Fact]
    public async Task Antennas_returns_active_entries_sorted_by_label()
    {
        var response = await AuthenticatedClient().GetAsync("/api/v1/reference/antennas");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = ParseArray(await response.Content.ReadAsStringAsync());
        var labels = items.Select(e => e.GetProperty("label").GetString()!).ToList();

        labels.Should().Contain("Antenne 1");                       // active (semée par le fixture)
        labels.Should().NotContain("ZZZ Antenne inactive");         // inactive exclue (SC-002)
        labels.Should().BeInAscendingOrder();                       // tri stable (SC-004)
        items[0].GetProperty("code").GetString().Should().NotBeNull();
    }

    [Fact]
    public async Task Reference_endpoint_without_token_returns_401()
    {
        var response = await _fixture.CreateClient().GetAsync("/api/v1/reference/antennas");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("civilities")]
    [InlineData("cities")]
    [InlineData("districts")]
    public async Task Simple_nomenclatures_return_active_items(string path)
    {
        var response = await AuthenticatedClient().GetAsync($"/api/v1/reference/{path}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = ParseArray(await response.Content.ReadAsStringAsync());
        items.Should().NotBeEmpty();
        items[0].TryGetProperty("label", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Countries_expose_nationality_and_exclude_inactive()
    {
        var response = await AuthenticatedClient().GetAsync("/api/v1/reference/countries");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = ParseArray(await response.Content.ReadAsStringAsync());
        var countries = items.Select(e => e.GetProperty("country").GetString()).ToList();

        countries.Should().Contain("Côte d'Ivoire");
        countries.Should().NotContain("Pays inactif");            // inactif exclu
        items.First(e => e.GetProperty("code").GetString() == "CI")
            .GetProperty("nationality").GetString().Should().Be("Ivoirienne");
    }
}
