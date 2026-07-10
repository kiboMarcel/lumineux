using Lumineux.Domain.Abstractions;
using Lumineux.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lumineux.Infrastructure.Repositories;

/// <summary>
/// Droits effectifs d'un membre = **union dédupliquée** des droits portés par ses **profils du bureau**
/// (features 004/011). **Seule** source de vérité des droits (feature 029 : mécanisme hérité retiré).
/// </summary>
public sealed class EffectivePermissionsReader : IEffectivePermissionsReader
{
    private readonly AppDbContext _db;

    public EffectivePermissionsReader(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(int memberId, CancellationToken ct = default) =>
        await (
            from mbp in _db.MemberBureauProfiles.AsNoTracking()
            join bpp in _db.BureauProfilePermissions.AsNoTracking()
                on mbp.BureauProfileId equals bpp.BureauProfileId
            where mbp.MemberId == memberId
            select bpp.Permission).Distinct().ToListAsync(ct);
}
