using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Reports;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Reports;

/// <summary>Cas d'usage : synthèse d'affluence par antenne sur une période (feature 018, US1).</summary>
public sealed class GetAntennaAttendanceSummaryHandler
{
    private readonly IAttendanceReportRepository _reports;
    private readonly ICurrentUser _user;
    private readonly IValidator<ReportPeriod> _validator;

    public GetAntennaAttendanceSummaryHandler(
        IAttendanceReportRepository reports, ICurrentUser user, IValidator<ReportPeriod> validator)
    {
        _reports = reports;
        _user = user;
        _validator = validator;
    }

    public async Task<AntennaAttendanceSummaryResponse> HandleAsync(
        DateTime from, DateTime to, int? antennaId, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(new ReportPeriod(from, to), ct);

        if (!_user.HasPermission(Permissions.ManageAttendance))
        {
            throw new ForbiddenException("Droit requis pour consulter les rapports de présence.");
        }

        var rows = await _reports.GetAntennaSummaryAsync(from, to, antennaId, ct);
        var items = rows
            .Select(r => new AntennaAttendanceSummaryItem(
                r.AntennaId,
                r.AntennaLabel,
                r.SessionCount,
                r.ValidAttendanceCount,
                r.SessionCount == 0 ? 0m : Math.Round((decimal)r.ValidAttendanceCount / r.SessionCount, 2)))
            .ToList();

        return new AntennaAttendanceSummaryResponse(from, to, items);
    }
}
