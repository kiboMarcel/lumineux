using Lumineux.Application.Contracts.Auth;
using Lumineux.Application.Contracts.Setup;
using Lumineux.Application.Setup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumineux.Api.Controllers;

[ApiController]
[Route("api/v1/setup")]
public sealed class SetupController : ControllerBase
{
    private readonly InstallFirstAdminHandler _installFirstAdmin;
    private readonly GetSetupStatusHandler _status;

    public SetupController(InstallFirstAdminHandler installFirstAdmin, GetSetupStatusHandler status)
    {
        _installFirstAdmin = installFirstAdmin;
        _status = status;
    }

    /// <summary>
    /// Indiquer si l'instance est déjà installée (feature 012). Route anonyme, lecture seule ;
    /// renvoie un simple booléen (aucune donnée sensible). Sert à la SPA pour afficher le lien
    /// « Première installation » uniquement sur une instance vierge.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SetupStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SetupStatusResponse>> Status(CancellationToken ct) =>
        Ok(await _status.HandleAsync(ct));

    /// <summary>
    /// Installer le premier administrateur (feature 005, FR-001). Route anonyme unique, autobloquée
    /// dès qu'un membre actif dispose du droit `manage_bureau_profiles`.
    /// </summary>
    [HttpPost("first-admin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TokenResponse>> InstallFirstAdmin(
        [FromBody] InstallFirstAdminRequest request, CancellationToken ct)
    {
        var result = await _installFirstAdmin.HandleAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
