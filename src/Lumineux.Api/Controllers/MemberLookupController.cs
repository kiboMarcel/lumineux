using Lumineux.Application.Contracts.Members;
using Lumineux.Application.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumineux.Api.Controllers;

/// <summary>
/// Recherche membre allégée (feature 015) — vue minimale d'identification pour l'ajout manuel de
/// présence. Accès réservé (côté handler) à <c>manage_attendance</c> OU <c>manage_members</c> ; ce
/// contrôleur est distinct de <see cref="MembersController"/> pour ne pas hériter de sa politique
/// <c>manage_members</c>.
/// </summary>
[ApiController]
[Route("api/v1/members/lookup")]
[Authorize]
public sealed class MemberLookupController : ControllerBase
{
    private readonly LookupMembersHandler _lookup;

    public MemberLookupController(LookupMembersHandler lookup) => _lookup = lookup;

    /// <summary>Recherche des membres par référence/nom (champs minimaux, résultats plafonnés).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MemberLookupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MemberLookupResponse>>> Lookup(
        [FromQuery] string? query, CancellationToken ct) =>
        Ok(await _lookup.HandleAsync(query, ct));
}
