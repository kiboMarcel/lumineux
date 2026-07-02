using System.Security.Cryptography;
using System.Text;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Infrastructure.Security;

/// <summary>
/// Jeton QR rotatif façon TOTP : jeton = HMAC-SHA256(secret, compteur_temps) tronqué à 8 chiffres.
/// Une photo du QR devient invalide après ~<c>stepSeconds</c>. Le secret ne quitte jamais le serveur.
/// </summary>
public sealed class QrTokenService : IQrTokenService
{
    private static readonly DateTime Epoch = DateTime.UnixEpoch;

    public string GenerateSecret() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public QrToken GetCurrentToken(string secret, int stepSeconds, DateTime nowUtc)
    {
        var counter = ToCounter(nowUtc, stepSeconds);
        var token = Compute(secret, counter);
        var expiresAt = Epoch.AddSeconds((counter + 1) * stepSeconds);
        return new QrToken(token, stepSeconds, expiresAt);
    }

    public bool Validate(string secret, int stepSeconds, string token, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var counter = ToCounter(nowUtc, stepSeconds);
        // Tolérance ± 1 pas pour absorber la dérive et la latence.
        foreach (var c in new[] { counter, counter - 1, counter + 1 })
        {
            if (FixedTimeEquals(Compute(secret, c), token))
            {
                return true;
            }
        }

        return false;
    }

    private static long ToCounter(DateTime utc, int stepSeconds)
    {
        var seconds = (long)(utc.ToUniversalTime() - Epoch).TotalSeconds;
        return seconds / stepSeconds;
    }

    private static string Compute(string secret, long counter)
    {
        var key = Convert.FromBase64String(secret);
        using var hmac = new HMACSHA256(key);
        var message = BitConverter.GetBytes(counter);
        var hash = hmac.ComputeHash(message);

        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
                     | (hash[offset + 1] << 16)
                     | (hash[offset + 2] << 8)
                     | hash[offset + 3];
        var otp = binary % 100_000_000;
        return otp.ToString("D8");
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
