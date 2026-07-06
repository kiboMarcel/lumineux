using FluentAssertions;
using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Reports;
using Lumineux.Domain.Abstractions;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

/// <summary>Cas d'usage du taux d'assiduité par membre (feature 018, US2).</summary>
public sealed class MemberAttendanceRateTests
{
    private static readonly DateTime From = new(2026, 6, 1);
    private static readonly DateTime To = new(2026, 6, 30);

    private readonly IAttendanceReportRepository _reports = Substitute.For<IAttendanceReportRepository>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();

    private GetMemberAttendanceRateHandler Handler() =>
        new(_reports, _user, new ReportPeriodValidator());

    [Fact]
    public async Task Computes_rate_as_fraction()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        _reports.GetMemberRateDataAsync(7, From, To, Arg.Any<CancellationToken>())
            .Returns(new MemberRateData("Jane Doe", OriginAntennaId: 1, ValidAttendanceCount: 3, EligibleSessionCount: 4));

        var result = await Handler().HandleAsync(7, From, To);

        result.MemberFullName.Should().Be("Jane Doe");
        result.Rate.Should().Be(0.75m);
        result.EligibleSessionCount.Should().Be(4);
    }

    [Fact]
    public async Task No_eligible_session_yields_zero_rate_without_dividing_by_zero()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        _reports.GetMemberRateDataAsync(7, From, To, Arg.Any<CancellationToken>())
            .Returns(new MemberRateData("Jane Doe", OriginAntennaId: null, ValidAttendanceCount: 0, EligibleSessionCount: 0));

        var result = await Handler().HandleAsync(7, From, To);

        result.Rate.Should().Be(0m);
    }

    [Fact]
    public async Task Unknown_member_returns_not_found()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        _reports.GetMemberRateDataAsync(Arg.Any<int>(), From, To, Arg.Any<CancellationToken>())
            .Returns((MemberRateData?)null);

        var act = () => Handler().HandleAsync(404, From, To);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Refuses_without_manage_attendance()
    {
        _user.HasPermission(Arg.Any<string>()).Returns(false);

        var act = () => Handler().HandleAsync(7, From, To);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
