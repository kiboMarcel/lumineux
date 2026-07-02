namespace Lumineux.Application.Abstractions;

/// <summary>Contexte de l'utilisateur authentifié (résolu depuis le jeton JWT côté API).</summary>
public interface ICurrentUser
{
    int? MemberId { get; }

    string? UserName { get; }

    bool IsAuthenticated { get; }

    bool HasPermission(string permission);
}
