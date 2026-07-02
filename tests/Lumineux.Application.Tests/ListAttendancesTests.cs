using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Attendances;
using Lumineux.Application.Contracts.Attendances;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class ListAttendancesTests
{
    private static readonly DateTime Now = new(2026, 7, 5, 9, 20, 0, DateTimeKind.Utc);

    private readonly IAttendanceSessionRepository _sessions = Substitute.For<IAttendanceSessionRepository>();
    private readonly IAttendanceRepository _attendances = Substitute.For<IAttendanceRepository>();
    private readonly IMemberReadRepository _members = Substitute.For<IMemberReadRepository>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();

    private ListAttendancesHandler CreateHandler() => new(_sessions, _attendances, _members, _user);

    private static AttendanceSession Session()
    {
        var session = AttendanceSession.Start(1, Now.Date, 1, "secret", 30, Now);
        session.Id = 10;
        return session;
    }

    private void GivenBureauAndSession()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        _sessions.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Session());
    }

    [Fact]
    public async Task List_returns_items_with_member_names_and_valid_count()
    {
        GivenBureauAndSession();
        var items = new List<Attendance>
        {
            Attendance.RecordScan(10, 1, Now, null),
            Attendance.RecordManual(10, 2, Now, null),
        };
        _attendances.ListBySessionAsync(Arg.Any<int>(), Arg.Any<AttendanceStatus?>(), Arg.Any<CancellationToken>())
            .Returns(items);
        _attendances.CountValidBySessionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(2);
        _members.GetByIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, Member>
            {
                [1] = new() { Id = 1, FirstName = "Ana", LastName = "One" },
                [2] = new() { Id = 2, FirstName = "Bob", LastName = "Two" },
            });

        var result = await CreateHandler().HandleAsync(1, statusFilter: null);

        result.ValidCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.MemberFullName).Should().Contain(new[] { "Ana One", "Bob Two" });
    }

    [Fact]
    public async Task List_with_all_filter_passes_null_status_to_repository()
    {
        GivenBureauAndSession();
        _attendances.ListBySessionAsync(Arg.Any<int>(), Arg.Any<AttendanceStatus?>(), Arg.Any<CancellationToken>())
            .Returns(new List<Attendance>());
        _members.GetByIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, Member>());

        await CreateHandler().HandleAsync(1, AttendanceStatusFilter.All);

        await _attendances.Received().ListBySessionAsync(Arg.Any<int>(), null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_without_permission_throws_forbidden()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(false);

        var act = () => CreateHandler().HandleAsync(1, null);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
