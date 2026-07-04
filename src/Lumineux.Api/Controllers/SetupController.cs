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

    public SetupController(InstallFirstAdminHandler installFirstAdmin)
    {
        _installFirstAdmin = installFirstAdmin;
    }

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
