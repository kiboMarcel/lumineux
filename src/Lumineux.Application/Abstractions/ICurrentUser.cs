namespace Lumineux.Application.Abstractions;

/// <summary>Contexte de l'utilisateur authentifié (résolu depuis le jeton JWT côté API).</summary>
public interface ICurrentUser
{
    int? MemberId { get; }

    string? UserName { get; }

    bool IsAuthenticated { get; }

    /// <summary>
    /// Droits fonctionnels portés par la session courante (claims <c>permission</c> du jeton).
    /// Collection vide si aucun droit ; jamais <c>null</c>. Reflète l'état au moment de l'émission
    /// du jeton (feature 007).
    /// </summary>
    IReadOnlyCollection<string> Permissions { get; }

    bool HasPermission(string permission);
}
