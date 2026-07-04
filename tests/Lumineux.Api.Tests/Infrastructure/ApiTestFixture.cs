using System.Collections.Concurrent;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Infrastructure.Persistence;
using Lumineux.Infrastructure.Persistence.Interceptors;
using Lumineux.Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lumineux.Api.Tests.Infrastructure;

/// <summary>
/// Envoi d'e-mail de test capturant le dernier lien de réinitialisation (feature 006), sans jamais
/// l'écrire ailleurs. Équivalent fonctionnel de <c>LoggingEmailSender</c> pour l'invitation.
/// </summary>
public sealed class CapturingResetEmailSender : IEmailSender
{
    private readonly ConcurrentDictionary<string, string> _linksByEmail = new();

    public int ResetSendCount;

    public string? LastResetLink { get; private set; }

    public string? ResetLinkFor(string email) =>
        _linksByEmail.TryGetValue(email, out var link) ? link : null;

    public Task<EmailSendOutcome> SendInvitationAsync(
        string? toEmail, string loginId, string temporaryPassword, CancellationToken ct = default) =>
        Task.FromResult(string.IsNullOrWhiteSpace(toEmail) ? EmailSendOutcome.NoRecipient : EmailSendOutcome.Sent);

    public Task<EmailSendOutcome> SendPasswordResetAsync(string? toEmail, string resetLink, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return Task.FromResult(EmailSendOutcome.NoRecipient);
        }

        Interlocked.Increment(ref ResetSendCount);
        _linksByEmail[toEmail] = resetLink;
        LastResetLink = resetLink;
        return Task.FromResult(EmailSendOutcome.Sent);
    }
}

/// <summary>
/// Fabrique de test : démarre l'API avec une base SQLite en mémoire (schéma créé via EnsureCreated)
/// et une antenne d'amorçage. Fournit l'émission de jetons JWT de test.
/// </summary>
public sealed class ApiTestFixture : WebApplicationFactory<Program>
{
    public const string SigningKey = "integration-tests-signing-key-please-override-0123456789";
    public const int SeededAntennaId = 1;

    /// <summary>Id du membre actif amorcé (utilisé pour les jetons de membre des tests de scan).</summary>
    public int SeededMemberId { get; private set; }

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    /// <summary>E-mail sender de test capturant les liens de réinitialisation (feature 006).</summary>
    public CapturingResetEmailSender Email { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "Lumineux",
                ["Jwt:Audience"] = "Lumineux",
                ["Jwt:SigningKey"] = SigningKey,
                ["ConnectionStrings:Default"] = "DataSource=:memory:",
                ["AutoClose:Enabled"] = "false",
            }));

        builder.ConfigureServices(services =>
        {
            // Retire toutes les inscriptions EF liées à AppDbContext (provider SQL Server)
            // pour les remplacer par SQLite en mémoire.
            var efDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(AppDbContext)
                    || (d.ServiceType.FullName?.Contains("DbContextOptions", StringComparison.Ordinal) == true
                        && (d.ServiceType.FullName.Contains(nameof(AppDbContext), StringComparison.Ordinal)
                            || d.ServiceType == typeof(DbContextOptions))))
                .ToList();

            foreach (var descriptor in efDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseSqlite(_connection);
                options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
            });

            // Feature 006 : capture des liens de réinitialisation (aucun SMTP en test).
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Email);

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();

            if (!db.Antennas.Any())
            {
                db.Antennas.Add(new Antenna { Code = "A1", Label = "Antenne 1", District = 1, Status = "Active" });
                db.SaveChanges();
            }

            var member = db.Members.FirstOrDefault();
            if (member is null)
            {
                member = new Member
                {
                    Reference = "M-FIXTURE-0001",
                    EntryDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Gender = "F",
                    LastName = "Doe",
                    FirstName = "Jane",
                    Status = "Active",
                    AntennaId = db.Antennas.First().Id,
                };
                db.Members.Add(member);
                db.SaveChanges();
            }

            SeededMemberId = member.Id;
        });
    }

    public string IssueBureauToken() =>
        Issue(memberId: 42, "bureau", Lumineux.Application.Abstractions.Permissions.ManageAttendance);

    /// <summary>Jeton d'un membre du bureau disposant du droit de gestion des membres (feature 002).</summary>
    public string IssueMembersManagerToken() =>
        Issue(memberId: 43, "bureau-membres", Lumineux.Application.Abstractions.Permissions.ManageMembers);

    /// <summary>Jeton d'un administrateur des profils du bureau (feature 004).</summary>
    public string IssueBureauProfilesAdminToken() =>
        Issue(memberId: 44, "bureau-profils", Lumineux.Application.Abstractions.Permissions.ManageBureauProfiles);

    public string IssueMemberToken()
    {
        _ = Services; // force la construction de l'hôte (et l'amorçage) avant de lire SeededMemberId
        return Issue(SeededMemberId, "membre");
    }

    /// <summary>Émet un jeton de membre (sans droit de gestion) pour un id donné — utilisé par les tests de charge.</summary>
    public string IssueMemberToken(int memberId) => Issue(memberId, "membre");

    private string Issue(int memberId, string name, params string[] permissions)
    {
        using var scope = Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<TestTokenIssuer>();
        return issuer.Issue(memberId, name, permissions);
    }

    /// <summary>Amorce un membre + compte ACTIF avec un mot de passe connu (et droits éventuels).</summary>
    public async Task<int> SeedActiveMemberAccountAsync(string reference, string password, params string[] permissions)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<Lumineux.Domain.Abstractions.IPasswordHasher>();

        var member = NewMember(reference, db.Antennas.First().Id);
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var account = MemberAccount.Provision(member, hasher.Hash(password));
        account.ChangePassword(hasher.Hash(password)); // lève mustChangePassword
        account.Activate();
        db.MemberAccounts.Add(account);
        await db.SaveChangesAsync();

        // Feature 004 : les droits sont désormais portés par des profils. Pour préserver la
        // sémantique historique de cette méthode, on obtient-ou-crée un profil par droit demandé
        // (nom stable « Test/<permission> ») et on l'attribue au membre.
        if (permissions.Length > 0)
        {
            var catalog = scope.ServiceProvider.GetRequiredService<Lumineux.Domain.Abstractions.IPermissionCatalog>();
            foreach (var permission in permissions.Distinct(StringComparer.Ordinal))
            {
                var profileName = "Test/" + permission;
                var normalized = profileName.ToLowerInvariant();
                var profile = await db.BureauProfiles
                    .FirstOrDefaultAsync(x => x.NameNormalized == normalized);
                if (profile is null)
                {
                    profile = BureauProfile.Create(profileName, null, new[] { permission }, catalog);
                    db.BureauProfiles.Add(profile);
                    await db.SaveChangesAsync();
                }

                var alreadyAssigned = await db.MemberBureauProfiles
                    .AnyAsync(x => x.MemberId == member.Id && x.BureauProfileId == profile.Id);
                if (!alreadyAssigned)
                {
                    db.MemberBureauProfiles.Add(new MemberBureauProfile
                    {
                        MemberId = member.Id,
                        BureauProfileId = profile.Id,
                    });
                }
            }
            await db.SaveChangesAsync();
        }

        return member.Id;
    }

    /// <summary>
    /// Réinitialise l'état d'installation (feature 005) : supprime tous les profils, attributions,
    /// comptes de connexion et membres autres que ceux d'amorçage du fixture. Permet à chaque test
    /// qui exerce `/setup/first-admin` de repartir sur une base « vierge côté admins ».
    /// </summary>
    public async Task ResetInstallationStateAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.MemberBureauProfiles.RemoveRange(db.MemberBureauProfiles);
        db.BureauProfilePermissions.RemoveRange(db.BureauProfilePermissions);
        db.BureauProfiles.RemoveRange(db.BureauProfiles);
        db.MemberPermissions.RemoveRange(db.MemberPermissions);
        db.MemberAccounts.RemoveRange(db.MemberAccounts);

        var seededId = SeededMemberId;
        var extraMembers = db.Members.Where(m => m.Id != seededId).ToList();
        db.Members.RemoveRange(extraMembers);

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Amorce un membre + compte ACTIF en contrôlant l'e-mail et le statut du membre (feature 006).
    /// Retourne l'id du membre. Un e-mail nul et/ou un statut ≠ Active rendent le compte inéligible
    /// à la réinitialisation (FR-011/012).
    /// </summary>
    public async Task<int> SeedMemberAccountAsync(string reference, string password, string? email, string status = "Active")
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var member = NewMember(reference, db.Antennas.First().Id);
        member.Email = email;
        member.Status = status;
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var account = MemberAccount.Provision(member, hasher.Hash(password));
        account.ChangePassword(hasher.Hash(password)); // lève mustChangePassword
        account.Activate();
        db.MemberAccounts.Add(account);
        await db.SaveChangesAsync();

        return member.Id;
    }

    /// <summary>
    /// Sème un jeton de réinitialisation valide (ou expiré) pour le compte d'un membre et retourne le
    /// jeton EN CLAIR (feature 006). Permet de tester la réinitialisation indépendamment de la demande.
    /// </summary>
    public async Task<string> SeedResetTokenAsync(int memberId, bool expired = false)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IResetTokenService>();

        var account = await db.MemberAccounts.FirstAsync(a => a.MemberId == memberId);
        var (clear, hash) = tokenService.Generate();
        // Un jeton "expiré" est émis dans le passé (émis -60 min, durée 30 min → déjà périmé).
        var issuedAt = expired ? DateTime.UtcNow.AddMinutes(-60) : DateTime.UtcNow;
        var token = PasswordResetToken.Issue(account, hash, issuedAt, 30);
        db.PasswordResetTokens.Add(token);
        await db.SaveChangesAsync();

        return clear;
    }

    /// <summary>Amorce un membre + compte EN ATTENTE d'activation avec un mot de passe temporaire connu.</summary>
    public async Task<int> SeedPendingMemberAccountAsync(string reference, string temporaryPassword)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<Lumineux.Domain.Abstractions.IPasswordHasher>();

        var member = NewMember(reference, db.Antennas.First().Id);
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var account = MemberAccount.Provision(member, hasher.Hash(temporaryPassword));
        db.MemberAccounts.Add(account);
        await db.SaveChangesAsync();

        return member.Id;
    }

    private static Member NewMember(string reference, int antennaId) => new()
    {
        Reference = reference,
        EntryDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Gender = "F",
        LastName = "Auth",
        FirstName = reference,
        Status = "Active",
        AntennaId = antennaId,
    };

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
