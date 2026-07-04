using Lumineux.Application.BureauProfiles;
using Lumineux.Application.Contracts.BureauProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumineux.Api.Controllers;

[ApiController]
[Route("api/v1/members/{memberId:int}/bureau-profiles")]
[Authorize]
public sealed class MemberBureauProfilesController : ControllerBase
{
    private readonly AssignProfileHandler _assign;
    private readonly RevokeProfileHandler _revoke;
    private readonly GetMemberProfilesHandler _get;

    public MemberBureauProfilesController(
        AssignProfileHandler assign,
        RevokeProfileHandler revoke,
        GetMemberProfilesHandler get)
    {
        _assign = assign;
        _revoke = revoke;
        _get = get;
    }

    /// <summary>Consulter les profils d'un membre et ses droits effectifs (US4, FR-009).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(MemberProfilesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberProfilesResponse>> Get(int memberId, CancellationToken ct) =>
        Ok(await _get.HandleAsync(memberId, ct));

    /// <summary>Attribuer un profil du bureau à un membre (US2, FR-004/FR-014). Idempotent.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Assign(int memberId, [FromBody] AssignProfileRequest request, CancellationToken ct)
    {
        await _assign.HandleAsync(memberId, request, ct);
        return NoContent();
    }

    /// <summary>Révoquer une attribution (US3, FR-004). Garde-fou dernier administrateur (FR-012a).</summary>
    [HttpDelete("{profileId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Revoke(int memberId, int profileId, CancellationToken ct)
    {
        await _revoke.HandleAsync(memberId, profileId, ct);
        return NoContent();
    }
}
