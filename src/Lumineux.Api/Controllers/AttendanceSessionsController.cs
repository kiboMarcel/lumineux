using Lumineux.Application.Abstractions;
using Lumineux.Application.AttendanceSessions;
using Lumineux.Application.Contracts.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumineux.Api.Controllers;

[ApiController]
[Route("api/v1/attendance-sessions")]
[Authorize(Policy = Permissions.ManageAttendance)]
public sealed class AttendanceSessionsController : ControllerBase
{
    private readonly StartSessionHandler _startSession;
    private readonly GetSessionHandler _getSession;
    private readonly GetCurrentQrTokenHandler _getQrToken;
    private readonly CloseSessionHandler _closeSession;
    private readonly ListMyOpenSessionsHandler _myOpenSessions;

    public AttendanceSessionsController(
        StartSessionHandler startSession,
        GetSessionHandler getSession,
        GetCurrentQrTokenHandler getQrToken,
        CloseSessionHandler closeSession,
        ListMyOpenSessionsHandler myOpenSessions)
    {
        _startSession = startSession;
        _getSession = getSession;
        _getQrToken = getQrToken;
        _closeSession = closeSession;
        _myOpenSessions = myOpenSessions;
    }

    /// <summary>Démarre une session de présence (FR-001..003).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Start([FromBody] StartSessionRequest request, CancellationToken ct)
    {
        var session = await _startSession.HandleAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { sessionId = session.Id }, session);
    }

    /// <summary>Récupère les sessions encore ouvertes démarrées par l'utilisateur courant (feature 023, reprise).</summary>
    [HttpGet("mine/open")]
    [ProducesResponseType(typeof(IReadOnlyList<SessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<SessionResponse>>> MyOpenSessions(CancellationToken ct) =>
        Ok(await _myOpenSessions.HandleAsync(ct));

    /// <summary>Consulte une session.</summary>
    [HttpGet("{sessionId:int}")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionResponse>> Get(int sessionId, CancellationToken ct) =>
        Ok(await _getSession.HandleAsync(sessionId, ct));

    /// <summary>Récupère le jeton QR courant à afficher (FR-013).</summary>
    [HttpGet("{sessionId:int}/qr")]
    [ProducesResponseType(typeof(QrTokenResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<QrTokenResponse>> GetQr(int sessionId, CancellationToken ct) =>
        Ok(await _getQrToken.HandleAsync(sessionId, ct));

    /// <summary>Clôture la session et propage l'heure de fin aux présences valides (FR-005..007).</summary>
    [HttpPost("{sessionId:int}/close")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionResponse>> Close(int sessionId, CancellationToken ct) =>
        Ok(await _closeSession.HandleAsync(sessionId, ct));
}
