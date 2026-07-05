using Lumineux.Application.Contracts.Reference;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Reference;

/// <summary>
/// Cas d'usage : lecture des nomenclatures pour les listes de sélection (feature 010). Projette les
/// entités actives (triées, fournies par le repository) vers des DTO dédiés. Aucune entité exposée.
/// </summary>
public sealed class GetReferenceDataHandler
{
    private readonly IReferenceDataRepository _repository;

    public GetReferenceDataHandler(IReferenceDataRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<ReferenceItemResponse>> GetAntennasAsync(CancellationToken ct = default) =>
        (await _repository.GetActiveAntennasAsync(ct))
            .Select(a => new ReferenceItemResponse(a.Id, a.Code, a.Label))
            .ToList();

    public async Task<IReadOnlyList<ReferenceItemResponse>> GetCivilitiesAsync(CancellationToken ct = default) =>
        (await _repository.GetActiveCivilitiesAsync(ct))
            .Select(c => new ReferenceItemResponse(c.Id, c.Code, c.Label))
            .ToList();

    public async Task<IReadOnlyList<ReferenceItemResponse>> GetCitiesAsync(CancellationToken ct = default) =>
        (await _repository.GetActiveCitiesAsync(ct))
            .Select(c => new ReferenceItemResponse(c.Id, c.Code, c.Label))
            .ToList();

    public async Task<IReadOnlyList<ReferenceItemResponse>> GetDistrictsAsync(CancellationToken ct = default) =>
        (await _repository.GetActiveDistrictsAsync(ct))
            .Select(d => new ReferenceItemResponse(d.Id, d.Code, d.Label))
            .ToList();

    public async Task<IReadOnlyList<CountryResponse>> GetCountriesAsync(CancellationToken ct = default) =>
        (await _repository.GetActiveCountriesAsync(ct))
            .Select(c => new CountryResponse(c.Id, c.Code, c.LabelCountry, c.LabelNationality))
            .ToList();
}
