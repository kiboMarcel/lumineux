using Lumineux.Domain.Abstractions;
using Lumineux.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lumineux.Infrastructure.Repositories;

public sealed class AntennaReadRepository : IAntennaReadRepository
{
    private readonly AppDbContext _db;

    public AntennaReadRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsAsync(int antennaId, CancellationToken ct = default) =>
        _db.Antennas.AnyAsync(x => x.Id == antennaId, ct);
}
