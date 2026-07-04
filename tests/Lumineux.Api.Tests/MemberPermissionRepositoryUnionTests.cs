using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Lumineux.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lumineux.Api.Tests;

/// <summary>
/// Vérifie que le refactor de <see cref="IMemberPermissionRepository"/> (feature 004, T013) renvoie
/// l'union des droits issus des profils du membre, sans doublon (FR-006, SC-005).
/// </summary>
public sealed class MemberPermissionRepositoryUnionTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public MemberPermissionRepositoryUnionTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GetPermissions_returns_union_without_duplicates()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var catalog = scope.ServiceProvider.GetRequiredService<IPermissionCatalog>();
        var repo = scope.ServiceProvider.GetRequiredService<IMemberPermissionRepository>();

        var member = new Member
        {
            Reference = "UNION-" + suffix,
            EntryDate = DateTime.UtcNow,
            Gender = "F",
            LastName = "Union",
            FirstName = suffix,
            Status = MemberStatuses.Active,
            AntennaId = db.Antennas.First().Id,
        };
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var pAtt = BureauProfile.Create("Union-Att-" + suffix, null, new[] { Permissions.ManageAttendance }, catalog);
        var pMemAtt = BureauProfile.Create("Union-Both-" + suffix, null,
            new[] { Permissions.ManageMembers, Permissions.ManageAttendance }, catalog);
        db.BureauProfiles.AddRange(pAtt, pMemAtt);
        await db.SaveChangesAsync();

        db.MemberBureauProfiles.AddRange(
            new MemberBureauProfile { MemberId = member.Id, BureauProfileId = pAtt.Id },
            new MemberBureauProfile { MemberId = member.Id, BureauProfileId = pMemAtt.Id });
        await db.SaveChangesAsync();

        var perms = await repo.GetPermissionsAsync(member.Id);

        perms.Should().BeEquivalentTo(new[] { Permissions.ManageAttendance, Permissions.ManageMembers });
        perms.Distinct().Count().Should().Be(perms.Count); // pas de doublon
    }

    [Fact]
    public async Task GetPermissions_returns_empty_when_no_profile()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IMemberPermissionRepository>();

        var member = new Member
        {
            Reference = "NOPERM-" + Guid.NewGuid().ToString("N")[..8],
            EntryDate = DateTime.UtcNow,
            Gender = "F",
            LastName = "NoPerm",
            FirstName = "X",
            Status = MemberStatuses.Active,
            AntennaId = db.Antennas.First().Id,
        };
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var perms = await repo.GetPermissionsAsync(member.Id);

        perms.Should().BeEmpty();
    }
}
