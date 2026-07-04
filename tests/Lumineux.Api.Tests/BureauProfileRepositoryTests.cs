using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Lumineux.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lumineux.Api.Tests;

/// <summary>
/// Verrouille la brique du garde-fou triple FR-012 : le comptage précis des administrateurs actifs.
/// Les tests utilisent des deltas (baseline) car le fixture SQLite est partagé.
/// </summary>
public sealed class BureauProfileRepositoryTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public BureauProfileRepositoryTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CountActiveAdministrators_counts_only_distinct_active_members()
    {
        var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var catalog = scope.ServiceProvider.GetRequiredService<IPermissionCatalog>();
        var repo = scope.ServiceProvider.GetRequiredService<IBureauProfileRepository>();

        var baseline = await repo.CountActiveAdministratorsAsync();

        var m1 = await SeedMemberAsync(db, "REPO-M1-" + suffix, active: true);
        var m2 = await SeedMemberAsync(db, "REPO-M2-" + suffix, active: true);
        var mInactive = await SeedMemberAsync(db, "REPO-M3-" + suffix, active: false);

        var admin = BureauProfile.Create("Admin-" + suffix, null,
            new[] { Permissions.ManageBureauProfiles }, catalog);
        var secondary = BureauProfile.Create("Admin2-" + suffix, null,
            new[] { Permissions.ManageBureauProfiles, Permissions.ManageMembers }, catalog);
        db.BureauProfiles.AddRange(admin, secondary);
        await db.SaveChangesAsync();

        db.MemberBureauProfiles.AddRange(
            new MemberBureauProfile { MemberId = m1.Id, BureauProfileId = admin.Id },
            new MemberBureauProfile { MemberId = m2.Id, BureauProfileId = admin.Id },
            new MemberBureauProfile { MemberId = m2.Id, BureauProfileId = secondary.Id },
            new MemberBureauProfile { MemberId = mInactive.Id, BureauProfileId = admin.Id });
        await db.SaveChangesAsync();

        var after = await repo.CountActiveAdministratorsAsync();

        (after - baseline).Should().Be(2); // m1 + m2 (déduplication) ; mInactive exclu
    }

    [Fact]
    public async Task CountActiveAdministrators_excludeProfileId_simulates_permission_removal()
    {
        var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var catalog = scope.ServiceProvider.GetRequiredService<IPermissionCatalog>();
        var repo = scope.ServiceProvider.GetRequiredService<IBureauProfileRepository>();

        var m1 = await SeedMemberAsync(db, "REPO-EX-M1-" + suffix, active: true);
        var profile = BureauProfile.Create("SoleAdmin-" + suffix, null,
            new[] { Permissions.ManageBureauProfiles }, catalog);
        db.BureauProfiles.Add(profile);
        await db.SaveChangesAsync();
        db.MemberBureauProfiles.Add(new MemberBureauProfile { MemberId = m1.Id, BureauProfileId = profile.Id });
        await db.SaveChangesAsync();

        var withExclusion = await repo.CountActiveAdministratorsAsync(excludeProfileId: profile.Id);
        var withMemberExclusion = await repo.CountActiveAdministratorsAsync(excludeMemberId: m1.Id);
        var withoutExclusion = await repo.CountActiveAdministratorsAsync();

        // m1 n'est admin QUE via ce profil et n'a pas d'autre attribution admin ici.
        // withExclusion et withMemberExclusion NE DOIVENT PAS inclure m1.
        (withoutExclusion - withExclusion).Should().Be(1);
        (withoutExclusion - withMemberExclusion).Should().Be(1);
    }

    [Fact]
    public async Task CountActiveAdministrators_ignores_inactive_members()
    {
        var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var catalog = scope.ServiceProvider.GetRequiredService<IPermissionCatalog>();
        var repo = scope.ServiceProvider.GetRequiredService<IBureauProfileRepository>();

        var baseline = await repo.CountActiveAdministratorsAsync();

        var inactive = await SeedMemberAsync(db, "REPO-INA-" + suffix, active: false);
        var profile = BureauProfile.Create("Admin-INA-" + suffix, null,
            new[] { Permissions.ManageBureauProfiles }, catalog);
        db.BureauProfiles.Add(profile);
        await db.SaveChangesAsync();
        db.MemberBureauProfiles.Add(new MemberBureauProfile { MemberId = inactive.Id, BureauProfileId = profile.Id });
        await db.SaveChangesAsync();

        var after = await repo.CountActiveAdministratorsAsync();

        (after - baseline).Should().Be(0); // inactif ignoré
    }

    private static async Task<Member> SeedMemberAsync(AppDbContext db, string reference, bool active)
    {
        var member = new Member
        {
            Reference = reference,
            EntryDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = "F",
            LastName = "Repo",
            FirstName = reference,
            Status = active ? MemberStatuses.Active : "Archived",
            AntennaId = db.Antennas.First().Id,
        };
        db.Members.Add(member);
        await db.SaveChangesAsync();
        return member;
    }
}
