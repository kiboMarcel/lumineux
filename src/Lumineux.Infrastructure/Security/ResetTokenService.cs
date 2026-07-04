using System.Security.Cryptography;
using System.Text;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Infrastructure.Security;

/// <summary>
/// Génère un jeton de réinitialisation de 32 octets aléatoires (256 bits d'entropie, FR-015) encodé
/// en base64url pour l'insertion dans une URL. L'empreinte persistée est un SHA-256 : un hachage
/// rapide suffit car le jeton a une entropie maximale (rien à bruteforcer) — voir research.md §1.
/// </summary>
public sealed class ResetTokenService : IResetTokenService
{
    private const int TokenBytes = 32;

    public (string ClearToken, string TokenHash) Generate()
    {
        var clear = Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));
        return (clear, Hash(clear));
    }

    public string Hash(string clearToken)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(clearToken));
        return Convert.ToBase64String(digest);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
