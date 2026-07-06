using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Reports;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Reports;

/// <summary>Cas d'usage : taux d'assiduité d'un membre sur une période (feature 018, US2).</summary>
public sealed class GetMemberAttendanceRateHandler
{
    private readonly IAttendanceReportRepository _reports;
    private readonly ICurrentUser _user;
    private readonly IValidator<ReportPeriod> _validator;

    public GetMemberAttendanceRateHandler(
        IAttendanceReportRepository reports, ICurrentUser user, IValidator<ReportPeriod> validator)
    {
        _reports = reports;
        _user = user;
        _validator = validator;
    }

    public async Task<MemberAttendanceRateResponse> HandleAsync(
        int memberId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(new ReportPeriod(from, to), ct);

        if (!_user.HasPermission(Permissions.ManageAttendance))
        {
            throw new ForbiddenException("Droit requis pour consulter les rapports de présence.");
        }

        var data = await _reports.GetMemberRateDataAsync(memberId, from, to, ct)
            ?? throw new NotFoundException("Membre introuvable.");

        // Taux (fraction 0..1) sans division par zéro : 0 si aucune session éligible.
        var rate = data.EligibleSessionCount == 0
            ? 0m
            : Math.Round((decimal)data.ValidAttendanceCount / data.EligibleSessionCount, 4);

        return new MemberAttendanceRateResponse(
            memberId, data.MemberFullName, from, to,
            data.ValidAttendanceCount, data.EligibleSessionCount, rate);
    }
}
