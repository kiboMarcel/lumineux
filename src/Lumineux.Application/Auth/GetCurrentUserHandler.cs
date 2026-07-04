using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Auth;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Auth;

/// <summary>
/// Cas d'usage : lire le profil de session de l'utilisateur courant (feature 007, US1, FR-001..008).
/// Lecture pure dérivée du contexte de session (<see cref="ICurrentUser"/>) : identité minimale +
/// droits effectifs. Aucun accès base, aucun effet de bord. Les droits retournés sont exactement
/// ceux portés par le jeton courant (cohérence avec l'autorisation de l'API, FR-006).
/// </summary>
public sealed class GetCurrentUserHandler
{
    private const string GenericFailure = "Non authentifié.";

    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;

    public GetCurrentUserHandler(ICurrentUser user, IAuditLogger audit)
    {
        _user = user;
        _audit = audit;
    }

    public CurrentUserResponse Handle()
    {
        // Garde défensive : contexte authentifié mais sans identifiant de membre exploitable
        // (jeton malformé). Refus 401 générique journalisé, sans secret (FR-009).
        if (_user.MemberId is not int memberId)
        {
            _audit.Refused("CurrentUser", "Contexte utilisateur absent ou incomplet");
            throw new UnauthorizedException(GenericFailure);
        }

        var displayName = _user.UserName ?? string.Empty;
        var permissions = _user.Permissions.ToArray();

        return new CurrentUserResponse(memberId, displayName, permissions);
    }
}
