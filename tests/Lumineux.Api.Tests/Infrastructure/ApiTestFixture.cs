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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
