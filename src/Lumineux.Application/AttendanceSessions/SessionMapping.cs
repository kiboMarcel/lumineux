using Lumineux.Application.Contracts.Sessions;
using Lumineux.Domain.Entities;

namespace Lumineux.Application.AttendanceSessions;

internal static class SessionMapping
{
    public static SessionResponse ToResponse(this AttendanceSession s, int attendanceCount = 0) =>
        new(
            s.Id,
            s.AntennaId,
            s.MeetingDate,
            s.StartTime,
            s.EndTime,
            s.Status.ToString(),
            s.OpenedByMemberId,
            s.ClosedByMemberId,
            attendanceCount,
            s.SessionType.ToString());
}
