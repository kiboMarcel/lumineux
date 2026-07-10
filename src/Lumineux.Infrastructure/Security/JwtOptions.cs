namespace Lumineux.Infrastructure.Security;

/// <summary>Paramètres JWT (liés à la section "Jwt" de la configuration).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Lumineux";

    public string Audience { get; set; } = "Lumineux";

    /// <summary>Clé de signature symétrique. Fournie via secrets/variables d'environnement (jamais en dur).</summary>
    public string SigningKey { get; set; } = string.Empty;

    // La durée de vie du jeton est unifiée sur Auth:AccessTokenMinutes (dette m3) — plus de clé
    // Jwt:ExpirationMinutes redondante.
}
