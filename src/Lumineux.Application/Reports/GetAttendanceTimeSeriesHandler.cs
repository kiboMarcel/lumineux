using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Reports;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Reports;

/// <summary>
/// Cas d'usage : série temporelle des présences valides (feature 020, US1/US2). Agrège par intervalle
/// (semaine ISO / mois) sur une période, filtrable par antenne, en série **continue** (zéros inclus).
/// </summary>
public sealed class GetAttendanceTimeSeriesHandler
{
    private readonly IAttendanceReportRepository _reports;
    private readonly ICurrentUser _user;
    private readonly IValidator<ReportPeriod> _validator;

    public GetAttendanceTimeSeriesHandler(
        IAttendanceReportRepository reports, ICurrentUser user, IValidator<ReportPeriod> validator)
    {
        _reports = reports;
        _user = user;
        _validator = validator;
    }

    public async Task<AttendanceTimeSeriesResponse> HandleAsync(
        DateTime from, DateTime to, TimeSeriesGranularity granularity, int? antennaId, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(new ReportPeriod(from, to), ct);

        if (!Enum.IsDefined(granularity))
        {
            throw new DomainException("Granularité non supportée (semaine ou mois uniquement).");
        }

        if (!_user.HasPermission(Permissions.ManageAttendance))
        {
            throw new ForbiddenException("Droit requis pour consulter les rapports de présence.");
        }

        var sessions = await _reports.GetSessionValidCountsAsync(from, to, antennaId, ct);

        // Agrégation en mémoire par intervalle (date de réunion → intervalle).
        var aggregated = new Dictionary<DateTime, (int Valid, int Sessions)>();
        foreach (var s in sessions)
        {
            var key = TimeBuckets.BucketStart(s.MeetingDate, granularity);
            aggregated.TryGetValue(key, out var cur); // absent → (0, 0)
            aggregated[key] = (cur.Valid + s.ValidAttendanceCount, cur.Sessions + 1);
        }

        // Série continue : tous les intervalles de la plage, zéros inclus.
        var points = TimeBuckets.Generate(from, to, granularity)
            .Select(b =>
            {
                aggregated.TryGetValue(b.PeriodStart, out var agg); // absent → (0, 0)
                return new TimeSeriesPoint(b.PeriodStart, b.Label, agg.Valid, agg.Sessions);
            })
            .ToList();

        return new AttendanceTimeSeriesResponse(from, to, granularity, points);
    }
}
