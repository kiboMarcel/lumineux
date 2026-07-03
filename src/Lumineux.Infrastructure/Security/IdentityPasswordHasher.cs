using System.Security.Cryptography;
using Lumineux.Domain.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Lumineux.Infrastructure.Security;

/// <summary>
/// Hachage de mot de passe via <see cref="PasswordHasher{T}"/> (PBKDF2). Le clair n'est jamais
/// persisté. Fournit aussi la génération d'un mot de passe temporaire aléatoire.
/// </summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private static readonly object Subject = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(Subject, password);

    public bool Verify(string password, string hash) =>
        _hasher.VerifyHashedPassword(Subject, hash, password) != PasswordVerificationResult.Failed;

    public string GenerateTemporaryPassword()
    {
        // ~12 caractères base64url issus de 9 octets aléatoires cryptographiques.
        var bytes = RandomNumberGenerator.GetBytes(9);
        return Convert.ToBase64String(bytes)
            .Replace('+', 'A').Replace('/', 'B').Replace("=", string.Empty, StringComparison.Ordinal);
    }
}
