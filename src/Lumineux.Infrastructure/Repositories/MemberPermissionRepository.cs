using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lumineux.Infrastructure.Repositories;

/// <summary>
/// Lit les droits effectifs d'un membre en agrégeant l'union des permissions issues des profils
/// du bureau qui lui sont attribués (feature 004, FR-006). La table <c>member_permissions</c> est
/// conservée en lecture pour le seul <see cref="Security.BureauProfilesBootstrapper"/> (migration
/// au démarrage) et pour le repli <see cref="Security.PermissionBootstrapper"/>.
/// </summary>
public sealed class MemberPermissionRepository : IMemberPermissionRepository
{
    private readonly AppDbContext _db;

    public MemberPermissionRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(int memberId, CancellationToken ct = default)
    {
        var permissions = await (
            from mbp in _db.MemberBureauProfiles.AsNoTracking()
            join bpp in _db.BureauProfilePermissions.AsNoTracking()
                on mbp.BureauProfileId equals bpp.BureauProfileId
            where mbp.MemberId == memberId
            select bpp.Permission).Distinct().ToListAsync(ct);

        return permissions;
    }

    public Task<bool> HasPermissionAsync(int memberId, string permission, CancellationToken ct = default) =>
        _db.MemberPermissions.AnyAsync(x => x.MemberId == memberId && x.Permission == permission, ct);

    public async Task AddAsync(MemberPermission permission, CancellationToken ct = default) =>
        await _db.MemberPermissions.AddAsync(permission, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
