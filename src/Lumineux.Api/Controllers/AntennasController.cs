using Lumineux.Application.Abstractions;
using Lumineux.Application.Antennas;
using Lumineux.Application.Contracts.Antennas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumineux.Api.Controllers;

/// <summary>
/// Gestion des antennes (feature 016) : création, modification, activation/désactivation et liste de
/// gestion (inactives incluses). Réservé au droit <c>manage_referentials</c> (l'API reste l'autorité).
/// La lecture publique des antennes actives (feature 010) reste séparée et inchangée.
/// </summary>
[ApiController]
[Route("api/v1/antennas")]
[Authorize(Policy = Permissions.ManageReferentials)]
public sealed class AntennasController : ControllerBase
{
    private readonly CreateAntennaHandler _create;
    private readonly GetAntennaHandler _get;
    private readonly UpdateAntennaHandler _update;
    private readonly SetAntennaActiveHandler _setActive;
    private readonly ListAntennasHandler _list;

    public AntennasController(
        CreateAntennaHandler create,
        GetAntennaHandler get,
        UpdateAntennaHandler update,
        SetAntennaActiveHandler setActive,
        ListAntennasHandler list)
    {
        _create = create;
        _get = get;
        _update = update;
        _setActive = setActive;
        _list = list;
    }

    /// <summary>Crée une antenne (US1, FR-001).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AntennaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateAntennaRequest request, CancellationToken ct)
    {
        var result = await _create.HandleAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    /// <summary>Liste de gestion : toutes les antennes, inactives incluses (US4, FR-008).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AntennaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AntennaResponse>>> List(CancellationToken ct) =>
        Ok(await _list.HandleAsync(ct));

    /// <summary>Consulte une antenne par identifiant.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AntennaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AntennaResponse>> Get(int id, CancellationToken ct) =>
        Ok(await _get.HandleAsync(id, ct));

    /// <summary>Modifie le libellé et le district (le code est immuable) (US2, FR-004).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AntennaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AntennaResponse>> Update(
        int id, [FromBody] UpdateAntennaRequest request, CancellationToken ct) =>
        Ok(await _update.HandleAsync(id, request, ct));

    /// <summary>Désactive une antenne (refusé si session ouverte) (US3, FR-005/005a).</summary>
    [HttpPost("{id:int}/deactivate")]
    [ProducesResponseType(typeof(AntennaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AntennaResponse>> Deactivate(int id, CancellationToken ct) =>
        Ok(await _setActive.HandleAsync(id, active: false, ct));

    /// <summary>Réactive une antenne (US3, FR-005).</summary>
    [HttpPost("{id:int}/activate")]
    [ProducesResponseType(typeof(AntennaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AntennaResponse>> Activate(int id, CancellationToken ct) =>
        Ok(await _setActive.HandleAsync(id, active: true, ct));
}
