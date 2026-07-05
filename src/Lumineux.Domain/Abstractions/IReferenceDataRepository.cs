using Lumineux.Domain.Entities;

namespace Lumineux.Domain.Abstractions;

/// <summary>
/// Lecture des nomenclatures (feature 010) pour alimenter les listes de sélection de la fiche membre.
/// Renvoie uniquement les entrées actives, triées par libellé. Lecture seule (aucune mutation).
/// </summary>
public interface IReferenceDataRepository
{
    Task<IReadOnlyList<Antenna>> GetActiveAntennasAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Civility>> GetActiveCivilitiesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<City>> GetActiveCitiesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<District>> GetActiveDistrictsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Country>> GetActiveCountriesAsync(CancellationToken ct = default);
}
