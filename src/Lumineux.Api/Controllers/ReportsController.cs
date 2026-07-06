using System.Text;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Reports;
using Lumineux.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumineux.Api.Controllers;

/// <summary>
/// Rapports & statistiques de présence (feature 018) en **lecture seule** : synthèse par antenne/période
/// (JSON + CSV) et taux d'assiduité par membre. Réservé au droit <c>manage_attendance</c> (l'API reste
/// l'autorité). Aucune écriture, aucune migration.
/// </summary>
[ApiController]
[Route("api/v1/reports/attendance")]
[Authorize(Policy = Permissions.ManageAttendance)]
public sealed class ReportsController : ControllerBase
{
    private readonly GetAntennaAttendanceSummaryHandler _summary;
    private readonly GetMemberAttendanceRateHandler _memberRate;
    private readonly ExportAntennaAttendanceCsvHandler _csv;
    private readonly GetAttendanceTimeSeriesHandler _timeSeries;

    public ReportsController(
        GetAntennaAttendanceSummaryHandler summary,
        GetMemberAttendanceRateHandler memberRate,
        ExportAntennaAttendanceCsvHandler csv,
        GetAttendanceTimeSeriesHandler timeSeries)
    {
        _summary = summary;
        _memberRate = memberRate;
        _csv = csv;
        _timeSeries = timeSeries;
    }

    /// <summary>Synthèse d'affluence par antenne sur une période (US1).</summary>
    [HttpGet("antenna-summary")]
    [ProducesResponseType(typeof(AntennaAttendanceSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AntennaAttendanceSummaryResponse>> AntennaSummary(
        [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int? antennaId, CancellationToken ct) =>
        Ok(await _summary.HandleAsync(from, to, antennaId, ct));

    /// <summary>Export CSV de la synthèse par antenne (US3) — mêmes chiffres que le JSON.</summary>
    [HttpGet("antenna-summary.csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AntennaSummaryCsv(
        [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int? antennaId, CancellationToken ct)
    {
        var csv = await _csv.HandleAsync(from, to, antennaId, ct);
        // UTF-8 avec BOM pour une ouverture correcte dans Excel (francophone).
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        var fileName = $"presence-antennes_{from:yyyyMMdd}_{to:yyyyMMdd}.csv";
        return File(bytes, "text/csv", fileName);
    }

    /// <summary>Série temporelle des présences valides (semaine ISO / mois) sur une période (feature 020).</summary>
    [HttpGet("time-series")]
    [ProducesResponseType(typeof(AttendanceTimeSeriesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AttendanceTimeSeriesResponse>> TimeSeries(
        [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] TimeSeriesGranularity granularity, [FromQuery] int? antennaId, CancellationToken ct) =>
        Ok(await _timeSeries.HandleAsync(from, to, granularity, antennaId, ct));

    /// <summary>Taux d'assiduité d'un membre sur une période (US2).</summary>
    [HttpGet("member-rate")]
    [ProducesResponseType(typeof(MemberAttendanceRateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MemberAttendanceRateResponse>> MemberRate(
        [FromQuery] int memberId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct) =>
        Ok(await _memberRate.HandleAsync(memberId, from, to, ct));
}
