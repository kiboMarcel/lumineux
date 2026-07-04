using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Lumineux.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Tests d'intégration de la route `/api/v1/setup/first-admin` (feature 005).
/// Chaque test appelle <see cref="ApiTestFixture.ResetInstallationStateAsync"/> pour partir d'une
/// base « vierge côté admins » — indépendant de l'ordre d'exécution xUnit.
/// </summary>
public sealed class SetupEndpointsTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public SetupEndpointsTests(ApiTestFixture fixture) => _fixture = fixture;

    private static object ValidPayload(string? emailSuffix = null) => new
    {
        lastName = "Kouassi",
        firstName = "Yao",
        gender = "M",
        password = "MotDePasse1",
        email = emailSuffix is null ? "yao-" + Guid.NewGuid().ToString("N")[..8] + "@example.com" : "yao-" + emailSuffix + "@example.com",
        mobile = (string?)null,
    };

    [Fact]
    public async Task Install_on_empty_base_returns_201_with_token()
    {
        await _fixture.ResetInstallationStateAsync();
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/setup/first-admin", ValidPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("password").And.NotContain("passwordHash");
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        doc.RootElement.GetProperty("tokenType").GetString().Should().Be("Bearer");
    }

    [Fact]
    public async Task Install_returned_token_grants_all_functional_permissions()
    {
        await _fixture.ResetInstallationStateAsync();
        var client = _fixture.CreateClient();

        var install = await client.PostAsJsonAsync("/api/v1/setup/first-admin", ValidPayload());
        install.StatusCode.Should().Be(HttpStatusCode.Created);
        var token = JsonDocument.Parse(await install.Content.ReadAsStringAsync())
            .RootElement.GetProperty("accessToken").GetString();

        var admin = _fixture.CreateClient();
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // manage_bureau_profiles : lister les profils.
        var profiles = await admin.GetAsync("/api/v1/bureau-profiles");
        profiles.StatusCode.Should().Be(HttpStatusCode.OK);

        // manage_members : lister les membres.
        var members = await admin.GetAsync("/api/v1/members");
        members.StatusCode.Should().Be(HttpStatusCode.OK);

        // manage_attendance : démarrer une session (endpoint qui exige explicitement ce droit).
        var startSession = await admin.PostAsJsonAsync("/api/v1/attendance-sessions", new
        {
            antennaId = ApiTestFixture.SeededAntennaId,
            step = 1,
            scheduledAt = DateTime.UtcNow.AddMinutes(5),
        });
        ((int)startSession.StatusCode).Should().NotBe((int)HttpStatusCode.Unauthorized);
        ((int)startSession.StatusCode).Should().NotBe((int)HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Install_after_first_install_returns_409_already_installed()
    {
        await _fixture.ResetInstallationStateAsync();
        var client = _fixture.CreateClient();

        var first = await client.PostAsJsonAsync("/api/v1/setup/first-admin", ValidPayload("first"));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/v1/setup/first-admin", ValidPayload("second"));
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var doc = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("already_installed");
    }

    [Fact]
    public async Task Install_after_first_install_with_invalid_payload_still_returns_409_not_400()
    {
        await _fixture.ResetInstallationStateAsync();
        var client = _fixture.CreateClient();

        var first = await client.PostAsJsonAsync("/api/v1/setup/first-admin", ValidPayload("prio"));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Payload volontairement INVALIDE (nom vide, mot de passe faible) : le refus prioritaire
        // (FR-005) doit gagner sur la validation → 409 already_installed, PAS 400.
        var invalid = await client.PostAsJsonAsync("/api/v1/setup/first-admin", new
        {
            lastName = "", firstName = "", gender = "M", password = "weak",
        });

        invalid.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var doc = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("already_installed");
    }

    [Fact]
    public async Task Install_reuses_existing_administrateur_profile_without_modification()
    {
        await _fixture.ResetInstallationStateAsync();

        // Seed d'un profil « Administrateur » avec description particulière et liste de droits
        // PARTIELLE (seul manage_bureau_profiles). Aucune attribution → pas d'admin actif encore.
        string originalDescription = "Description humaine préexistante — NE PAS ÉCRASER.";
        int existingProfileId;
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var catalog = scope.ServiceProvider.GetRequiredService<IPermissionCatalog>();
            var profile = BureauProfile.Create("Administrateur", originalDescription,
                new[] { Permissions.ManageBureauProfiles }, catalog);
            db.BureauProfiles.Add(profile);
            await db.SaveChangesAsync();
            existingProfileId = profile.Id;
        }

        var client = _fixture.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/setup/first-admin", new
        {
            lastName = "Idem",
            firstName = "Test",
            gender = "F",
            password = "MotDePasse1",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Vérifications : aucun doublon de profil, description et permissions INCHANGÉES.
        using var check = _fixture.Services.CreateScope();
        var db2 = check.ServiceProvider.GetRequiredService<AppDbContext>();
        var profiles = db2.BureauProfiles.Where(p => p.NameNormalized == "administrateur").ToList();
        profiles.Should().HaveCount(1);
        profiles[0].Id.Should().Be(existingProfileId);
        profiles[0].Description.Should().Be(originalDescription);
        var perms = db2.BureauProfilePermissions.Where(bp => bp.BureauProfileId == existingProfileId).ToList();
        perms.Should().ContainSingle().Which.Permission.Should().Be(Permissions.ManageBureauProfiles);

        // Le nouveau membre est bien attribué au profil existant.
        db2.MemberBureauProfiles.Where(m => m.BureauProfileId == existingProfileId).Should().HaveCount(1);
    }

    [Fact]
    public async Task Install_with_existing_contact_returns_409_contact_in_use()
    {
        await _fixture.ResetInstallationStateAsync();

        // Seed d'un membre actif portant l'email cible.
        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Members.Add(new Member
            {
                Reference = "COLL-" + Guid.NewGuid().ToString("N")[..6],
                EntryDate = DateTime.UtcNow,
                LastName = "Existant",
                FirstName = "Contact",
                Gender = "F",
                Status = MemberStatuses.Active,
                Email = "collision@example.com",
                AntennaId = db.Antennas.First().Id,
            });
            await db.SaveChangesAsync();
        }

        var client = _fixture.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/setup/first-admin", new
        {
            lastName = "Autre",
            firstName = "Yao",
            gender = "M",
            password = "MotDePasse1",
            email = "collision@example.com",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("contact_in_use");

        // Atomicité : aucun MemberAccount / BureauProfile créé pour l'admin refusé.
        using var check = _fixture.Services.CreateScope();
        var db2 = check.ServiceProvider.GetRequiredService<AppDbContext>();
        db2.BureauProfiles.Should().BeEmpty();
        db2.MemberAccounts.Should().BeEmpty();
    }
}
