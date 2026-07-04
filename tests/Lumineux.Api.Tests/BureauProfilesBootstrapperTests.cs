using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Lumineux.Infrastructure.Persistence;
using Lumineux.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumineux.Api.Tests;

/// <summary>
/// Vérifie la migration idempotente au démarrage (feature 004, FR-013 / T014). Utilise le fixture
/// SQLite existant pour disposer d'un DbContext + IPermissionCatalog.
/// </summary>
public sealed class BureauProfilesBootstrapperTests : IClassFixture<Infrastructure.ApiTestFixture>
{
    private readonly Infrastructure.ApiTestFixture _fixture;

    public BureauProfilesBootstrapperTests(Infrastructure.ApiTestFixture fixture) => _fixture = fixture;

    private BureauProfilesBootstrapper CreateWith(string? memberReference)
    {
        var options = Options.Create(new AuthOptions
        {
            Bootstrap = new BootstrapOptions { MemberReference = memberReference ?? string.Empty },
        });
        return new BureauProfilesBootstrapper(_fixture.Services.GetRequiredService<IServiceScopeFactory>(),
            options, NullLogger<BureauProfilesBootstrapper>.Instance);
    }

    private async Task<Member> SeedMemberAsync(string reference)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var member = new Member
        {
            Reference = reference,
            EntryDate = DateTime.UtcNow,
            Gender = "F",
            LastName = "Boot",
            FirstName = reference,
            Status = MemberStatuses.Active,
            AntennaId = db.Antennas.First().Id,
        };
        db.Members.Add(member);
        await db.SaveChangesAsync();
        return member;
    }

    private async Task ClearBootstrapProfileAsync()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.BureauProfiles
            .FirstOrDefaultAsync(x => x.NameNormalized == BureauProfilesBootstrapper.BootstrapProfileName.ToLowerInvariant());
        if (existing is not null)
        {
            db.BureauProfiles.Remove(existing);
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Bootstrap_creates_amorcage_profile_and_assigns_to_referenced_member()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await ClearBootstrapProfileAsync();
        var reference = "BOOT-M-" + suffix;
        var member = await SeedMemberAsync(reference);

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tracked = await db.Members.FirstAsync(m => m.Id == member.Id);
            // Simuler que le PermissionBootstrapper de la feature 003 a déjà posé une ligne.
            db.MemberPermissions.Add(new MemberPermission
            {
                MemberId = tracked.Id, Permission = Permissions.ManageAttendance,
            });
            // Le membre doit aussi avoir un MemberAccount pour que la référence soit résolvable.
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var account = MemberAccount.Provision(tracked, hasher.Hash("dummy"));
            account.ChangePassword(hasher.Hash("dummy"));
            account.Activate();
            db.MemberAccounts.Add(account);
            await db.SaveChangesAsync();
        }

        var bootstrapper = CreateWith(reference);
        await bootstrapper.StartAsync(CancellationToken.None);

        using var check = _fixture.Services.CreateScope();
        var db2 = check.ServiceProvider.GetRequiredService<AppDbContext>();
        var profile = await db2.BureauProfiles.Include(p => p.Permissions)
            .FirstOrDefaultAsync(p => p.Name == BureauProfilesBootstrapper.BootstrapProfileName);
        profile.Should().NotBeNull();
        profile!.Permissions.Select(p => p.Permission).Should().Contain(Permissions.ManageAttendance);

        var assigned = await db2.MemberBureauProfiles
            .AnyAsync(x => x.MemberId == member.Id && x.BureauProfileId == profile.Id);
        assigned.Should().BeTrue();
    }

    [Fact]
    public async Task Bootstrap_relaunch_is_idempotent()
    {
        var bootstrapper = CreateWith(memberReference: null);
        await bootstrapper.StartAsync(CancellationToken.None);
        await bootstrapper.StartAsync(CancellationToken.None);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.BureauProfiles.CountAsync(p => p.Name == BureauProfilesBootstrapper.BootstrapProfileName);
        count.Should().BeLessOrEqualTo(1);
    }

    [Fact]
    public async Task Bootstrap_does_nothing_when_no_member_permissions_present()
    {
        // Contexte propre : on efface le profil « Amorçage » puis on vide member_permissions et on lance.
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var existing = await db.BureauProfiles
                .FirstOrDefaultAsync(x => x.Name == BureauProfilesBootstrapper.BootstrapProfileName);
            if (existing is not null) db.BureauProfiles.Remove(existing);
            db.MemberPermissions.RemoveRange(db.MemberPermissions);
            await db.SaveChangesAsync();
        }

        var bootstrapper = CreateWith(memberReference: null);
        await bootstrapper.StartAsync(CancellationToken.None);

        using var check = _fixture.Services.CreateScope();
        var db2 = check.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = await db2.BureauProfiles
            .AnyAsync(p => p.Name == BureauProfilesBootstrapper.BootstrapProfileName);
        exists.Should().BeFalse();
    }
}
