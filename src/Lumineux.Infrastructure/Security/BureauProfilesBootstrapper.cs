using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lumineux.Infrastructure.Security;

/// <summary>
/// Migration idempotente au démarrage (feature 004, FR-013). Bascule les droits directs
/// (`member_permissions` — feature 003) vers un profil système « Amorçage » et l'assigne au membre
/// bootstrap. Aucune duplication en cas de relance ; ne fait rien si `member_permissions` est vide.
/// </summary>
public sealed class BureauProfilesBootstrapper : IHostedService
{
    public const string BootstrapProfileName = "Amorçage";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<AuthOptions> _options;
    private readonly ILogger<BureauProfilesBootstrapper> _logger;

    public BureauProfilesBootstrapper(
        IServiceScopeFactory scopeFactory,
        IOptions<AuthOptions> options,
        ILogger<BureauProfilesBootstrapper> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var catalog = scope.ServiceProvider.GetRequiredService<IPermissionCatalog>();
        var bootstrap = _options.Value.Bootstrap;

        // 1) Union des droits déjà stockés dans member_permissions (source héritée).
        var legacyRows = await db.MemberPermissions.AsNoTracking().ToListAsync(cancellationToken);
        if (legacyRows.Count == 0)
        {
            return;
        }

        var legacyPermissions = legacyRows
            .Select(x => x.Permission)
            .Where(catalog.Contains)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (legacyPermissions.Count == 0)
        {
            return;
        }

        // 2) Idempotence : ne rien faire si le profil existe déjà.
        var normalized = BootstrapProfileName.ToLowerInvariant();
        var existing = await db.BureauProfiles
            .FirstOrDefaultAsync(x => x.NameNormalized == normalized, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        // 3) Création du profil « Amorçage ».
        var profile = BureauProfile.Create(BootstrapProfileName,
            "Profil système créé automatiquement lors de la migration (feature 004).",
            legacyPermissions, catalog);
        await db.BureauProfiles.AddAsync(profile, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // 4) Attribution : membre référencé si connu, sinon tous les porteurs historiques.
        var memberIds = new HashSet<int>();
        if (!string.IsNullOrWhiteSpace(bootstrap.MemberReference))
        {
            var accountMember = await db.MemberAccounts.AsNoTracking()
                .Where(a => a.LoginId == bootstrap.MemberReference)
                .Select(a => (int?)a.MemberId)
                .FirstOrDefaultAsync(cancellationToken);
            if (accountMember is int id)
            {
                memberIds.Add(id);
            }
        }
        if (memberIds.Count == 0)
        {
            foreach (var id in legacyRows.Select(x => x.MemberId).Distinct())
            {
                memberIds.Add(id);
            }
        }

        foreach (var memberId in memberIds)
        {
            await db.MemberBureauProfiles.AddAsync(
                new MemberBureauProfile { MemberId = memberId, BureauProfileId = profile.Id },
                cancellationToken);
        }
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Migration profils du bureau : profil « {Profile} » créé ({PermissionCount} droits), assigné à {MemberCount} membre(s).",
            BootstrapProfileName, legacyPermissions.Count, memberIds.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
