using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Reports;
using Lumineux.Domain.Abstractions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

/// <summary>Cas d'usage de synthèse d'affluence par antenne (feature 018, US1).</summary>
public sealed class AntennaAttendanceSummaryTests
{
    private static readonly DateTime From = new(2026, 6, 1);
    private static readonly DateTime To = new(2026, 6, 30);

    private readonly IAttendanceReportRepository _reports = Substitute.For<IAttendanceReportRepository>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();

    private GetAntennaAttendanceSummaryHandler Handler() =>
        new(_reports, _user, new ReportPeriodValidator());

    [Fact]
    public async Task Computes_average_valid_per_session()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        _reports.GetAntennaSummaryAsync(From, To, null, Arg.Any<CancellationToken>())
            .Returns(new List<AntennaSummaryRow> { new(1, "Antenne 1", SessionCount: 2, ValidAttendanceCount: 3) });

        var result = await Handler().HandleAsync(From, To, null);

        var item = result.Items.Should().ContainSingle().Subject;
        item.AverageValidPerSession.Should().Be(1.5m);
        item.ValidAttendanceCount.Should().Be(3);
    }

    [Fact]
    public async Task Average_is_zero_when_no_session()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        _reports.GetAntennaSummaryAsync(From, To, null, Arg.Any<CancellationToken>())
            .Returns(new List<AntennaSummaryRow> { new(1, "Antenne 1", SessionCount: 0, ValidAttendanceCount: 0) });

        var result = await Handler().HandleAsync(From, To, null);

        result.Items.Single().AverageValidPerSession.Should().Be(0m);
    }

    [Fact]
    public async Task Rejects_invalid_period()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);

        var act = () => Handler().HandleAsync(To, From, null); // fin < début

        await act.Should().ThrowAsync<ValidationException>();
        await _reports.DidNotReceive().GetAntennaSummaryAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refuses_without_manage_attendance()
    {
        _user.HasPermission(Arg.Any<string>()).Returns(false);

        var act = () => Handler().HandleAsync(From, To, null);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
