using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Lumineux.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lumineux.Infrastructure.Repositories;

public sealed class BureauProfileRepository : IBureauProfileRepository
{
    private readonly AppDbContext _db;

    public BureauProfileRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(BureauProfile profile, CancellationToken ct = default) =>
        await _db.BureauProfiles.AddAsync(profile, ct);

    public Task<BureauProfile?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.BureauProfiles.Include(x => x.Permissions).FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<BureauProfile?> GetByNameNormalizedAsync(string nameNormalized, CancellationToken ct = default) =>
        _db.BureauProfiles.Include(x => x.Permissions)
            .FirstOrDefaultAsync(x => x.NameNormalized == nameNormalized, ct);

    public async Task<IReadOnlyList<BureauProfile>> ListAllAsync(CancellationToken ct = default) =>
        await _db.BureauProfiles.Include(x => x.Permissions).OrderBy(x => x.Name).ToListAsync(ct);

    public void Remove(BureauProfile profile) => _db.BureauProfiles.Remove(profile);

    public Task<int> CountAssignmentsAsync(int profileId, CancellationToken ct = default) =>
        _db.MemberBureauProfiles.CountAsync(x => x.BureauProfileId == profileId, ct);

    public async Task<int> CountActiveAdministratorsAsync(
        int? excludeProfileId = null,
        int? excludeMemberId = null,
        CancellationToken ct = default)
    {
        var query =
            from mbp in _db.MemberBureauProfiles.AsNoTracking()
            join bpp in _db.BureauProfilePermissions.AsNoTracking()
                on mbp.BureauProfileId equals bpp.BureauProfileId
            join member in _db.Members.AsNoTracking() on mbp.MemberId equals member.Id
            where bpp.Permission == Permissions.ManageBureauProfiles
                && member.Status == MemberStatuses.Active
            select new { mbp.MemberId, mbp.BureauProfileId };

        if (excludeProfileId is { } profileId)
        {
            query = query.Where(x => x.BureauProfileId != profileId);
        }
        if (excludeMemberId is { } memberId)
        {
            query = query.Where(x => x.MemberId != memberId);
        }

        return await query.Select(x => x.MemberId).Distinct().CountAsync(ct);
    }

    public async Task<IReadOnlyDictionary<int, int>> CountAssignmentsByProfileAsync(CancellationToken ct = default)
    {
        var counts = await _db.MemberBureauProfiles.AsNoTracking()
            .GroupBy(x => x.BureauProfileId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);
        return counts.ToDictionary(x => x.Key, x => x.Count);
    }

    public Task<MemberBureauProfile?> GetAssignmentAsync(int memberId, int profileId, CancellationToken ct = default) =>
        _db.MemberBureauProfiles.FirstOrDefaultAsync(
            x => x.MemberId == memberId && x.BureauProfileId == profileId, ct);

    public async Task<IReadOnlyList<MemberBureauProfile>> GetAssignmentsByProfileAsync(int profileId, CancellationToken ct = default) =>
        await _db.MemberBureauProfiles.AsNoTracking()
            .Where(x => x.BureauProfileId == profileId).ToListAsync(ct);

    public async Task<IReadOnlyList<BureauProfile>> GetProfilesForMemberAsync(int memberId, CancellationToken ct = default)
    {
        var profileIds = await _db.MemberBureauProfiles.AsNoTracking()
            .Where(x => x.MemberId == memberId)
            .Select(x => x.BureauProfileId)
            .ToListAsync(ct);

        if (profileIds.Count == 0)
        {
            return Array.Empty<BureauProfile>();
        }

        return await _db.BureauProfiles.AsNoTracking().Include(x => x.Permissions)
            .Where(x => profileIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    public async Task AddAssignmentAsync(MemberBureauProfile assignment, CancellationToken ct = default) =>
        await _db.MemberBureauProfiles.AddAsync(assignment, ct);

    public void RemoveAssignment(MemberBureauProfile assignment) =>
        _db.MemberBureauProfiles.Remove(assignment);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
