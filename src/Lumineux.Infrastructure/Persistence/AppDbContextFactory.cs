using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lumineux.Infrastructure.Persistence;

/// <summary>
/// Fabrique design-time pour les commandes EF (migrations). N'ouvre aucune connexion réelle ;
/// la chaîne de connexion effective est fournie à l'exécution par l'API.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("LUMINEUX_DB")
            ?? "Server=localhost;Database=Lumineux;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connection)
            .Options;

        return new AppDbContext(options);
    }
}
