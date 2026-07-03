using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lumineux.Infrastructure.Repositories;

public sealed class MemberAccountRepository : IMemberAccountRepository
{
    private readonly AppDbContext _db;

    public MemberAccountRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(MemberAccount account, CancellationToken ct = default) =>
        await _db.MemberAccounts.AddAsync(account, ct);

    public Task<MemberAccount?> GetByMemberIdAsync(int memberId, CancellationToken ct = default) =>
        _db.MemberAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.MemberId == memberId, ct);
}
