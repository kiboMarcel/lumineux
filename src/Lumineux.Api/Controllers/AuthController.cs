using Lumineux.Application.Auth;
using Lumineux.Application.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumineux.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginHandler _login;
    private readonly ActivateAccountHandler _activate;
    private readonly ChangePasswordHandler _changePassword;

    public AuthController(LoginHandler login, ActivateAccountHandler activate, ChangePasswordHandler changePassword)
    {
        _login = login;
        _activate = activate;
        _changePassword = changePassword;
    }

    /// <summary>Se connecter et obtenir un jeton d'accès (FR-001).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request, CancellationToken ct) =>
        Ok(await _login.HandleAsync(request, ct));

    /// <summary>Première connexion : changer le mot de passe temporaire et activer le compte (FR-007).</summary>
    [HttpPost("activate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TokenResponse>> Activate([FromBody] ActivateAccountRequest request, CancellationToken ct) =>
        Ok(await _activate.HandleAsync(request, ct));

    /// <summary>Changer son mot de passe (utilisateur connecté, FR-009).</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await _changePassword.HandleAsync(request, ct);
        return NoContent();
    }
}
