using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lumineux.Infrastructure.Repositories;

public sealed class MemberReadRepository : IMemberReadRepository
{
    private readonly AppDbContext _db;

    public MemberReadRepository(AppDbContext db) => _db = db;

    public Task<Member?> GetByIdAsync(int memberId, CancellationToken ct = default) =>
        _db.Members.FirstOrDefaultAsync(x => x.Id == memberId, ct);

    public async Task<IReadOnlyDictionary<int, Member>> GetByIdsAsync(IReadOnlyCollection<int> memberIds, CancellationToken ct = default)
    {
        if (memberIds.Count == 0)
        {
            return new Dictionary<int, Member>();
        }

        return await _db.Members
            .AsNoTracking()
            .Where(x => memberIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
    }
}
