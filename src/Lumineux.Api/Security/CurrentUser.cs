using System.Security.Claims;
using Lumineux.Application.Abstractions;

namespace Lumineux.Api.Security;

/// <summary>
/// Résout le contexte utilisateur depuis le jeton JWT de la requête courante.
/// Placé dans la couche API car lié au contexte HTTP (l'abstraction reste dans Application).
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public int? MemberId =>
        int.TryParse(Principal?.FindFirst("member_id")?.Value, out var id) ? id : null;

    public string? UserName => Principal?.Identity?.Name;

    public bool HasPermission(string permission) =>
        Principal?.Claims.Any(c => c.Type == "permission" && c.Value == permission) ?? false;
}
