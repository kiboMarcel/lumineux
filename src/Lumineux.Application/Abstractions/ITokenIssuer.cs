namespace Lumineux.Application.Abstractions;

/// <summary>Jeton d'accès émis (valeur + expiration).</summary>
public sealed record IssuedToken(string AccessToken, DateTime ExpiresAt);

/// <summary>Émission des jetons d'accès JWT (feature 003).</summary>
public interface ITokenIssuer
{
    IssuedToken Issue(int memberId, string userName, IReadOnlyCollection<string> permissions);
}
