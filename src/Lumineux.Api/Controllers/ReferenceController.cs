using Lumineux.Application.Contracts.Reference;
using Lumineux.Application.Reference;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumineux.Api.Controllers;

/// <summary>
/// Données de référence (nomenclatures) en lecture seule (feature 010), pour alimenter les listes de
/// sélection de la fiche membre. Accès réservé aux utilisateurs authentifiés (aucun droit de gestion
/// requis — nomenclatures non sensibles).
/// </summary>
[ApiController]
[Route("api/v1/reference")]
[Authorize]
public sealed class ReferenceController : ControllerBase
{
    private readonly GetReferenceDataHandler _reference;

    public ReferenceController(GetReferenceDataHandler reference) => _reference = reference;

    /// <summary>Lister les antennes actives (US1).</summary>
    [HttpGet("antennas")]
    [ProducesResponseType(typeof(IReadOnlyList<ReferenceItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ReferenceItemResponse>>> Antennas(CancellationToken ct) =>
        Ok(await _reference.GetAntennasAsync(ct));

    /// <summary>Lister les civilités actives.</summary>
    [HttpGet("civilities")]
    [ProducesResponseType(typeof(IReadOnlyList<ReferenceItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ReferenceItemResponse>>> Civilities(CancellationToken ct) =>
        Ok(await _reference.GetCivilitiesAsync(ct));

    /// <summary>Lister les villes actives.</summary>
    [HttpGet("cities")]
    [ProducesResponseType(typeof(IReadOnlyList<ReferenceItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ReferenceItemResponse>>> Cities(CancellationToken ct) =>
        Ok(await _reference.GetCitiesAsync(ct));

    /// <summary>Lister les districts actifs.</summary>
    [HttpGet("districts")]
    [ProducesResponseType(typeof(IReadOnlyList<ReferenceItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ReferenceItemResponse>>> Districts(CancellationToken ct) =>
        Ok(await _reference.GetDistrictsAsync(ct));

    /// <summary>Lister les pays / nationalités actifs.</summary>
    [HttpGet("countries")]
    [ProducesResponseType(typeof(IReadOnlyList<CountryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<CountryResponse>>> Countries(CancellationToken ct) =>
        Ok(await _reference.GetCountriesAsync(ct));
}
