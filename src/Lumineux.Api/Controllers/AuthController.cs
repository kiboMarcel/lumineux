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
    private readonly RequestPasswordResetHandler _forgotPassword;
    private readonly ResetPasswordHandler _resetPassword;
    private readonly GetCurrentUserHandler _currentUser;

    public AuthController(
        LoginHandler login,
        ActivateAccountHandler activate,
        ChangePasswordHandler changePassword,
        RequestPasswordResetHandler forgotPassword,
        ResetPasswordHandler resetPassword,
        GetCurrentUserHandler currentUser)
    {
        _login = login;
        _activate = activate;
        _changePassword = changePassword;
        _forgotPassword = forgotPassword;
        _resetPassword = resetPassword;
        _currentUser = currentUser;
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

    /// <summary>Demander la réinitialisation de son mot de passe (feature 006, FR-001). Réponse générique.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GenericMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GenericMessageResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct) =>
        Ok(await _forgotPassword.HandleAsync(request, ct));

    /// <summary>Réinitialiser son mot de passe avec le jeton reçu par email (feature 006, FR-005).</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _resetPassword.HandleAsync(request, ct);
        return NoContent();
    }

    /// <summary>
    /// Récupérer l'identité et les droits effectifs de la session courante (feature 007, FR-001).
    /// Réservé aux utilisateurs authentifiés ; aucun droit de gestion requis. Aucun secret exposé.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me() => Ok(_currentUser.Handle());

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
