namespace Lumineux.Domain.Abstractions;

/// <summary>
/// Génération et hachage des jetons de réinitialisation de mot de passe (feature 006). Le jeton en
/// clair sert uniquement à construire le lien envoyé par email ; seule son empreinte est persistée.
/// </summary>
public interface IResetTokenService
{
    /// <summary>
    /// Génère un jeton haute entropie (FR-015). Retourne le jeton en clair (pour le lien) et son
    /// empreinte (pour la base).
    /// </summary>
    (string ClearToken, string TokenHash) Generate();

    /// <summary>Recalcule l'empreinte d'un jeton présenté, pour la recherche par index (FR-016).</summary>
    string Hash(string clearToken);
}
