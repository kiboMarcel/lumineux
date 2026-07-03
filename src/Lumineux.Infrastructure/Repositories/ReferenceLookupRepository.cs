using Lumineux.Domain.Abstractions;
using Lumineux.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lumineux.Infrastructure.Repositories;

public sealed class ReferenceLookupRepository : IReferenceLookupRepository
{
    private readonly AppDbContext _db;

    public ReferenceLookupRepository(AppDbContext db) => _db = db;

    public Task<bool> AntennaExistsAsync(int id, CancellationToken ct = default) =>
        _db.Antennas.AnyAsync(x => x.Id == id, ct);

    public Task<bool> CivilityExistsAsync(int id, CancellationToken ct = default) =>
        _db.Civilities.AnyAsync(x => x.Id == id, ct);

    public Task<bool> CountryExistsAsync(int id, CancellationToken ct = default) =>
        _db.Countries.AnyAsync(x => x.Id == id, ct);

    public Task<bool> CityExistsAsync(int id, CancellationToken ct = default) =>
        _db.Cities.AnyAsync(x => x.Id == id, ct);

    public Task<bool> DistrictExistsAsync(int id, CancellationToken ct = default) =>
        _db.Districts.AnyAsync(x => x.Id == id, ct);

    public Task<bool> MemberExistsAsync(int id, CancellationToken ct = default) =>
        _db.Members.AnyAsync(x => x.Id == id, ct);
}
