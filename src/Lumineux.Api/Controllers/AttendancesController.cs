using Lumineux.Application.Abstractions;
using Lumineux.Application.Attendances;
using Lumineux.Application.Contracts.Attendances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumineux.Api.Controllers;

[ApiController]
[Route("api/v1/attendance-sessions/{sessionId:int}")]
[Authorize]
public sealed class AttendancesController : ControllerBase
{
    private readonly ScanAttendanceHandler _scan;
    private readonly SyncOfflineScansHandler _sync;
    private readonly AddManualAttendanceHandler _addManual;
    private readonly CancelAttendanceHandler _cancel;
    private readonly ListAttendancesHandler _list;

    public AttendancesController(
        ScanAttendanceHandler scan,
        SyncOfflineScansHandler sync,
        AddManualAttendanceHandler addManual,
        CancelAttendanceHandler cancel,
        ListAttendancesHandler list)
    {
        _scan = scan;
        _sync = sync;
        _addManual = addManual;
        _cancel = cancel;
        _list = list;
    }

    /// <summary>Enregistre sa présence en scannant le code QR (FR-008..013).</summary>
    [HttpPost("scan")]
    [ProducesResponseType(typeof(AttendanceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AttendanceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Scan(int sessionId, [FromBody] ScanRequest request, CancellationToken ct)
    {
        var result = await _scan.HandleAsync(sessionId, request, ct);
        return result.AlreadyPresent
            ? Ok(result.Attendance)
            : StatusCode(StatusCodes.Status201Created, result.Attendance);
    }

    /// <summary>Synchronise un lot de scans effectués hors ligne (FR-023).</summary>
    [HttpPost("scan/batch")]
    [ProducesResponseType(typeof(OfflineScanBatchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OfflineScanBatchResponse>> SyncBatch(
        int sessionId, [FromBody] OfflineScanBatchRequest request, CancellationToken ct) =>
        Ok(await _sync.HandleAsync(sessionId, request, ct));

    /// <summary>Ajoute manuellement une présence pour un membre non équipé (bureau, FR-014).</summary>
    [HttpPost("attendances")]
    [Authorize(Policy = Permissions.ManageAttendance)]
    [ProducesResponseType(typeof(AttendanceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AttendanceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddManual(int sessionId, [FromBody] ManualAttendanceRequest request, CancellationToken ct)
    {
        var result = await _addManual.HandleAsync(sessionId, request, ct);
        return result.AlreadyPresent
            ? Ok(result.Attendance)
            : StatusCode(StatusCodes.Status201Created, result.Attendance);
    }

    /// <summary>Liste les présences d'une session (bureau, FR-021/022).</summary>
    [HttpGet("attendances")]
    [Authorize(Policy = Permissions.ManageAttendance)]
    [ProducesResponseType(typeof(AttendanceListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceListResponse>> List(
        int sessionId, [FromQuery] string? status, CancellationToken ct) =>
        Ok(await _list.HandleAsync(sessionId, status, ct));

    /// <summary>Retire/annule la présence d'un membre tant que la session est ouverte (bureau, FR-016).</summary>
    [HttpDelete("attendances/{memberId:int}")]
    [Authorize(Policy = Permissions.ManageAttendance)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(int sessionId, int memberId, CancellationToken ct)
    {
        await _cancel.HandleAsync(sessionId, memberId, ct);
        return NoContent();
    }
}
