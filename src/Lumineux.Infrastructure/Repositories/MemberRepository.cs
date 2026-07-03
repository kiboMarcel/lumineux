using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Lumineux.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lumineux.Infrastructure.Repositories;

public sealed class MemberRepository : IMemberRepository
{
    private readonly AppDbContext _db;

    public MemberRepository(AppDbContext db) => _db = db;

    public Task<Member?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Members.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<MemberPage> SearchAsync(string? query, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var q = _db.Members.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(m => m.LastName.Contains(term) || m.FirstName.Contains(term) || m.Reference.Contains(term));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(m => m.LastName).ThenBy(m => m.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return new MemberPage(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<Member>> FindActiveByNameAsync(string lastName, string firstName, CancellationToken ct = default) =>
        await _db.Members.AsNoTracking()
            .Where(m => m.Status == MemberStatuses.Active
                        && m.LastName == lastName && m.FirstName == firstName)
            .ToListAsync(ct);

    public Task<bool> IsContactUsedByActiveAsync(string? email, string? mobile, int? excludeMemberId, CancellationToken ct = default)
    {
        var hasEmail = !string.IsNullOrWhiteSpace(email);
        var hasMobile = !string.IsNullOrWhiteSpace(mobile);
        if (!hasEmail && !hasMobile)
        {
            return Task.FromResult(false);
        }

        return _db.Members.AnyAsync(
            m => m.Status == MemberStatuses.Active
                 && (excludeMemberId == null || m.Id != excludeMemberId)
                 && ((hasEmail && m.Email == email) || (hasMobile && m.Mobile == mobile)),
            ct);
    }

    public async Task AddAsync(Member member, CancellationToken ct = default) =>
        await _db.Members.AddAsync(member, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException("Violation d'unicité (référence ou coordonnée déjà utilisée).");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }
}
