using Lumineux.Application.Contracts.Attendances;
using Lumineux.Domain.Entities;

namespace Lumineux.Application.Attendances;

internal static class AttendanceMapping
{
    public static AttendanceResponse ToResponse(this Attendance a, string? memberFullName = null) =>
        new(
            a.Id,
            a.SessionId,
            a.MemberId,
            memberFullName,
            a.ArrivalTime,
            a.EndTime,
            a.Source.ToString(),
            a.Status.ToString(),
            a.OriginAntennaId);
}
