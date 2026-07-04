using Lumineux.Application.BureauProfiles;
using Lumineux.Application.Contracts.BureauProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumineux.Api.Controllers;

[ApiController]
[Route("api/v1/bureau-profiles")]
[Authorize]
public sealed class BureauProfilesController : ControllerBase
{
    private readonly CreateBureauProfileHandler _create;
    private readonly UpdateBureauProfileHandler _update;
    private readonly DeleteBureauProfileHandler _delete;
    private readonly ListBureauProfilesHandler _list;
    private readonly GetBureauProfileHandler _get;

    public BureauProfilesController(
        CreateBureauProfileHandler create,
        UpdateBureauProfileHandler update,
        DeleteBureauProfileHandler delete,
        ListBureauProfilesHandler list,
        GetBureauProfileHandler get)
    {
        _create = create;
        _update = update;
        _delete = delete;
        _list = list;
        _get = get;
    }

    /// <summary>Lister les profils du bureau (US4, FR-009). Accès manage_bureau_profiles OU manage_members.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BureauProfileSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<BureauProfileSummaryResponse>>> List(CancellationToken ct) =>
        Ok(await _list.HandleAsync(ct));

    /// <summary>Détail d'un profil et de ses titulaires (US4, FR-009).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BureauProfileDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BureauProfileDetailResponse>> Get(int id, CancellationToken ct) =>
        Ok(await _get.HandleAsync(id, ct));

    /// <summary>Créer un profil du bureau (FR-001, US1).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BureauProfileDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BureauProfileDetailResponse>> Create(
        [FromBody] BureauProfileWriteRequest request, CancellationToken ct)
    {
        var result = await _create.HandleAsync(request, ct);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    /// <summary>Modifier un profil (FR-002, US1).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(BureauProfileDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BureauProfileDetailResponse>> Update(
        int id, [FromBody] BureauProfileWriteRequest request, CancellationToken ct) =>
        Ok(await _update.HandleAsync(id, request, ct));

    /// <summary>Supprimer un profil (FR-003 + garde-fou FR-012c, US1).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _delete.HandleAsync(id, ct);
        return NoContent();
    }
}
