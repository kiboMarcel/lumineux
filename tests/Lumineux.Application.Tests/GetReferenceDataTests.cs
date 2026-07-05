using FluentAssertions;
using Lumineux.Application.Reference;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

/// <summary>
/// Tests unitaires du cas d'usage « données de référence » (feature 010). Vérifient la projection des
/// entités (fournies actives et triées par le repository) vers les DTO, dont la nationalité distincte
/// pour les pays.
/// </summary>
public sealed class GetReferenceDataTests
{
    private readonly IReferenceDataRepository _repo = Substitute.For<IReferenceDataRepository>();

    private GetReferenceDataHandler CreateHandler() => new(_repo);

    [Fact]
    public async Task Antennas_are_projected_to_reference_items()
    {
        _repo.GetActiveAntennasAsync(Arg.Any<CancellationToken>()).Returns(new List<Antenna>
        {
            new() { Id = 1, Code = "A1", Label = "Antenne 1", Status = "Active" },
            new() { Id = 2, Code = "A2", Label = "Antenne 2", Status = "Active" },
        });

        var result = await CreateHandler().GetAntennasAsync();

        result.Should().HaveCount(2);
        result[0].Should().BeEquivalentTo(new { Id = 1, Code = "A1", Label = "Antenne 1" });
    }

    [Fact]
    public async Task Civilities_cities_districts_are_projected_to_reference_items()
    {
        _repo.GetActiveCivilitiesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Civility> { new() { Id = 5, Code = "MME", Label = "Madame", Status = "Active" } });
        _repo.GetActiveCitiesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<City> { new() { Id = 7, Code = "ABJ", Label = "Abidjan", Status = "Active" } });
        _repo.GetActiveDistrictsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<District> { new() { Id = 9, Code = "COC", Label = "Cocody", Status = "Active" } });

        (await CreateHandler().GetCivilitiesAsync())[0].Label.Should().Be("Madame");
        (await CreateHandler().GetCitiesAsync())[0].Id.Should().Be(7);
        (await CreateHandler().GetDistrictsAsync())[0].Code.Should().Be("COC");
    }

    [Fact]
    public async Task Countries_expose_distinct_country_and_nationality_labels()
    {
        _repo.GetActiveCountriesAsync(Arg.Any<CancellationToken>()).Returns(new List<Country>
        {
            new() { Id = 12, Code = "CI", LabelCountry = "Côte d'Ivoire", LabelNationality = "Ivoirienne", Status = "Active" },
        });

        var result = await CreateHandler().GetCountriesAsync();

        result.Should().ContainSingle();
        result[0].Country.Should().Be("Côte d'Ivoire");
        result[0].Nationality.Should().Be("Ivoirienne");
    }
}
