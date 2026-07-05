using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lumineux.Infrastructure.Repositories;

/// <summary>
/// Lecture des nomenclatures depuis la base (feature 010). Filtre les entrées actives et trie par
/// libellé, sans suivi (lecture seule). Aucune écriture.
/// </summary>
public sealed class ReferenceDataRepository : IReferenceDataRepository
{
    private const string ActiveStatus = "Active";

    private readonly AppDbContext _db;

    public ReferenceDataRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Antenna>> GetActiveAntennasAsync(CancellationToken ct = default) =>
        await _db.Antennas.AsNoTracking()
            .Where(x => x.Status == ActiveStatus)
            .OrderBy(x => x.Label)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Civility>> GetActiveCivilitiesAsync(CancellationToken ct = default) =>
        await _db.Civilities.AsNoTracking()
            .Where(x => x.Status == ActiveStatus)
            .OrderBy(x => x.Label)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<City>> GetActiveCitiesAsync(CancellationToken ct = default) =>
        await _db.Cities.AsNoTracking()
            .Where(x => x.Status == ActiveStatus)
            .OrderBy(x => x.Label)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<District>> GetActiveDistrictsAsync(CancellationToken ct = default) =>
        await _db.Districts.AsNoTracking()
            .Where(x => x.Status == ActiveStatus)
            .OrderBy(x => x.Label)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Country>> GetActiveCountriesAsync(CancellationToken ct = default) =>
        await _db.Countries.AsNoTracking()
            .Where(x => x.Status == ActiveStatus)
            .OrderBy(x => x.LabelCountry)
            .ToListAsync(ct);
}
