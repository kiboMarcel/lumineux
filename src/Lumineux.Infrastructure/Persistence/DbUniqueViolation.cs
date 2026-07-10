using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Lumineux.Infrastructure.Persistence;

/// <summary>
/// Détection d'une violation de contrainte d'unicité, **robuste et indépendante de la langue** :
/// codes d'erreur natifs SQL Server (2601 = index unique, 2627 = contrainte unique/PK). Repli sur
/// l'inspection du message pour les autres fournisseurs (ex. SQLite en test), qui n'exposent pas
/// ces codes. Remplace l'ancienne détection uniquement textuelle (dette m4).
/// </summary>
internal static class DbUniqueViolation
{
    public static bool Is(DbUpdateException ex)
    {
        // Chemin natif (SQL Server, production) : fiable, insensible au libellé/locale.
        if (ex.InnerException is SqlException sql)
        {
            return sql.Number is 2601 or 2627;
        }

        // Repli fournisseur (SQLite/autres) : le message contient « UNIQUE » ou « duplicate ».
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }
}
