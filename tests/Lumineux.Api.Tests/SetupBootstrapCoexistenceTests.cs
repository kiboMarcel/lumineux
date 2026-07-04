using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Lumineux.Infrastructure.Persistence;
using Lumineux.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

/// <summary>
/// Vérifie la **coexistence** de la route `/setup/first-admin` (feature 005) avec le mécanisme
/// d'amorçage `Auth:Bootstrap:*` (feature 003 + migration profil « Amorçage » feature 004) : si
/// un admin actif a été créé par les bootstrappers au démarrage, la route DOIT refuser (FR-012).
/// </summary>
public sealed class SetupBootstrapCoexistenceTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public SetupBootstrapCoexistenceTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Install_returns_409_when_bootstrap_has_already_seeded_an_admin()
    {
        await _fixture.ResetInstallationStateAsync();

        // 1) Simuler le seed du `PermissionBootstrapper` (feature 003) : un membre bootstrap +
        //    compte + une ligne `member_permissions` portant `manage_bureau_profiles`.
        int bootstrapMemberId;
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var member = new Member
            {
                Reference = "BOOT-COEX-" + Guid.NewGuid().ToString("N")[..6],
                EntryDate = DateTime.UtcNow,
                Gender = "M",
                LastName = "Boot",
                FirstName = "Coex",
                Status = MemberStatuses.Active,
                AntennaId = db.Antennas.First().Id,
            };
            db.Members.Add(member);
            await db.SaveChangesAsync();
            bootstrapMemberId = member.Id;

            var account = MemberAccount.Provision(member, hasher.Hash("Passw0rd"));
            account.ChangePassword(hasher.Hash("Passw0rd"));
            account.Activate();
            db.MemberAccounts.Add(account);

            db.MemberPermissions.Add(new MemberPermission
            {
                MemberId = member.Id, Permission = Permissions.ManageBureauProfiles,
            });
            await db.SaveChangesAsync();
        }

        // 2) Rejouer `BureauProfilesBootstrapper` (feature 004) : bascule `member_permissions` en
        //    profil « Amorçage » + attribution → le membre bootstrap devient admin actif.
        var bootstrapper = new BureauProfilesBootstrapper(
            _fixture.Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new AuthOptions
            {
                Bootstrap = new BootstrapOptions
                {
                    MemberReference = "BOOT-COEX-INEXISTANT",  // délibérément absent → fallback tous membres
                },
            }),
            NullLogger<BureauProfilesBootstrapper>.Instance);
        await bootstrapper.StartAsync(CancellationToken.None);

        // 3) Vérifier qu'un admin actif existe désormais.
        using (var scope = _fixture.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IBureauProfileRepository>();
            (await repo.CountActiveAdministratorsAsync()).Should().BeGreaterOrEqualTo(1);
        }

        // 4) L'appel à la route d'installation DOIT refuser avec `already_installed` (FR-012).
        var client = _fixture.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/setup/first-admin", new
        {
            lastName = "Setup",
            firstName = "Attempt",
            gender = "M",
            password = "MotDePasse1",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("already_installed");
    }
}
