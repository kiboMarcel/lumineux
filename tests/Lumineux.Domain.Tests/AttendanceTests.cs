using FluentAssertions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Xunit;

namespace Lumineux.Domain.Tests;

public sealed class AttendanceTests
{
    private static readonly DateTime Now = new(2026, 7, 5, 9, 5, 0, DateTimeKind.Utc);

    [Fact]
    public void RecordScan_creates_valid_qrscan_with_origin_snapshot()
    {
        var attendance = Attendance.RecordScan(sessionId: 10, memberId: 42, Now, originAntennaId: 5, clientOperationId: "op-1");

        attendance.Status.Should().Be(AttendanceStatus.Valid);
        attendance.Source.Should().Be(AttendanceSource.QrScan);
        attendance.ArrivalTime.Should().Be(Now);
        attendance.OriginAntennaId.Should().Be(5);
        attendance.ClientOperationId.Should().Be("op-1");
        attendance.EndTime.Should().BeNull();
    }

    [Fact]
    public void RecordManual_creates_valid_manual_attendance()
    {
        var attendance = Attendance.RecordManual(sessionId: 10, memberId: 42, Now, originAntennaId: null);

        attendance.Source.Should().Be(AttendanceSource.Manual);
        attendance.Status.Should().Be(AttendanceStatus.Valid);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public void RecordScan_with_invalid_ids_throws(int sessionId, int memberId)
    {
        var act = () => Attendance.RecordScan(sessionId, memberId, Now, null);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_marks_attendance_cancelled_and_is_idempotent_guarded()
    {
        var attendance = Attendance.RecordScan(10, 42, Now, null);

        attendance.Cancel();
        attendance.Status.Should().Be(AttendanceStatus.Cancelled);

        var act = () => attendance.Cancel();
        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void ApplyEndTime_sets_end_time()
    {
        var attendance = Attendance.RecordScan(10, 42, Now, null);

        attendance.ApplyEndTime(Now.AddHours(2));

        attendance.EndTime.Should().Be(Now.AddHours(2));
    }
}
