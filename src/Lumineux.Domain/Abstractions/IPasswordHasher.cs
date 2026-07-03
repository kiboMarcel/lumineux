namespace Lumineux.Domain.Abstractions;

/// <summary>Hachage/vérification de mot de passe (Constitution IV). Le clair n'est jamais persisté.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);

    /// <summary>Génère un mot de passe temporaire aléatoire sûr (transmis une seule fois).</summary>
    string GenerateTemporaryPassword();
}
