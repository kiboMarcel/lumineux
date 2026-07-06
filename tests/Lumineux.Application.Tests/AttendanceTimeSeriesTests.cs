using FluentAssertions;
using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Reports;
using Lumineux.Application.Reports;
using Lumineux.Domain.Abstractions;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

/// <summary>Cas d'usage de la série temporelle des présences (feature 020, US1/US2).</summary>
public sealed class AttendanceTimeSeriesTests
{
    private static readonly DateTime From = new(2026, 1, 1);
    private static readonly DateTime To = new(2026, 3, 31);

    private readonly IAttendanceReportRepository _reports = Substitute.For<IAttendanceReportRepository>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();

    private GetAttendanceTimeSeriesHandler Handler() =>
        new(_reports, _user, new ReportPeriodValidator());

    [Fact]
    public async Task Builds_continuous_monthly_series_with_zeros_for_empty_months()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        // Présences en janvier (3) et mars (5) ; février vide.
        _reports.GetSessionValidCountsAsync(From, To, null, Arg.Any<CancellationToken>())
            .Returns(new List<SessionValidCount>
            {
                new(new DateTime(2026, 1, 10), 3),
                new(new DateTime(2026, 3, 5), 5),
            });

        var result = await Handler().HandleAsync(From, To, TimeSeriesGranularity.Month, null);

        result.Points.Select(p => p.Label).Should().Equal("2026-01", "2026-02", "2026-03");
        result.Points.Select(p => p.ValidAttendanceCount).Should().Equal(3, 0, 5); // février = 0 (continu)
        result.Points.Select(p => p.SessionCount).Should().Equal(1, 0, 1);
    }

    [Fact]
    public async Task Rejects_unsupported_granularity()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);

        var act = () => Handler().HandleAsync(From, To, (TimeSeriesGranularity)99, null);

        await act.Should().ThrowAsync<DomainException>();
        await _reports.DidNotReceive().GetSessionValidCountsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_invalid_period()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);

        var act = () => Handler().HandleAsync(To, From, TimeSeriesGranularity.Month, null); // fin < début

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Refuses_without_manage_attendance()
    {
        _user.HasPermission(Arg.Any<string>()).Returns(false);

        var act = () => Handler().HandleAsync(From, To, TimeSeriesGranularity.Month, null);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Passes_antenna_filter_to_repository()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        _reports.GetSessionValidCountsAsync(From, To, 7, Arg.Any<CancellationToken>())
            .Returns(new List<SessionValidCount>());

        var result = await Handler().HandleAsync(From, To, TimeSeriesGranularity.Month, antennaId: 7);

        await _reports.Received(1).GetSessionValidCountsAsync(From, To, 7, Arg.Any<CancellationToken>());
        // Antenne sans donnée → série continue à 0.
        result.Points.Should().OnlyContain(p => p.ValidAttendanceCount == 0);
    }
}
