using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lumineux.Infrastructure.Repositories;

/// <summary>Dépôt de gestion des antennes (feature 016) : lecture (inactives incluses) + écriture.</summary>
public sealed class AntennaRepository : IAntennaRepository
{
    private readonly AppDbContext _db;

    public AntennaRepository(AppDbContext db) => _db = db;

    public Task<Antenna?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Antennas.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Antenna?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalized = (code ?? string.Empty).Trim();
        return _db.Antennas.FirstOrDefaultAsync(x => x.Code == normalized, ct);
    }

    public async Task<IReadOnlyList<Antenna>> ListAllAsync(CancellationToken ct = default) =>
        await _db.Antennas.OrderBy(x => x.Label).ToListAsync(ct);

    public async Task AddAsync(Antenna antenna, CancellationToken ct = default) =>
        await _db.Antennas.AddAsync(antenna, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
