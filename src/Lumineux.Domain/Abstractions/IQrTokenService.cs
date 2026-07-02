namespace Lumineux.Domain.Abstractions;

/// <summary>Jeton QR courant à afficher (fenêtre temporelle en cours).</summary>
public sealed record QrToken(string Token, int StepSeconds, DateTime ExpiresAt);

/// <summary>
/// Service de génération/validation du jeton QR rotatif (façon TOTP, voir research §3).
/// Le secret ne quitte jamais le serveur.
/// </summary>
public interface IQrTokenService
{
    /// <summary>Génère un secret aléatoire propre à une session.</summary>
    string GenerateSecret();

    /// <summary>Calcule le jeton de la fenêtre courante et sa fin de validité.</summary>
    QrToken GetCurrentToken(string secret, int stepSeconds, DateTime nowUtc);

    /// <summary>Valide un jeton scanné pour la fenêtre courante (± 1 pas de tolérance).</summary>
    bool Validate(string secret, int stepSeconds, string token, DateTime nowUtc);
}
