using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Attendances;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Enums;

namespace Lumineux.Application.Attendances;

/// <summary>
/// Cas d'usage : consultation des présences d'une session, en direct et après clôture (US3, FR-021/022).
/// </summary>
public sealed class ListAttendancesHandler
{
    private readonly IAttendanceSessionRepository _sessions;
    private readonly IAttendanceRepository _attendances;
    private readonly IMemberReadRepository _members;
    private readonly ICurrentUser _user;

    public ListAttendancesHandler(
        IAttendanceSessionRepository sessions,
        IAttendanceRepository attendances,
        IMemberReadRepository members,
        ICurrentUser user)
    {
        _sessions = sessions;
        _attendances = attendances;
        _members = members;
        _user = user;
    }

    public async Task<AttendanceListResponse> HandleAsync(int sessionId, string? statusFilter, CancellationToken ct = default)
    {
        if (!_user.HasPermission(Permissions.ManageAttendance))
        {
            throw new ForbiddenException("Droit requis pour gérer les présences.");
        }

        var session = await _sessions.GetByIdAsync(sessionId, ct)
            ?? throw new NotFoundException("Session introuvable.");

        var status = ParseFilter(statusFilter);
        var attendances = await _attendances.ListBySessionAsync(session.Id, status, ct);
        var validCount = await _attendances.CountValidBySessionAsync(session.Id, ct);

        var memberIds = attendances.Select(a => a.MemberId).Distinct().ToList();
        var members = await _members.GetByIdsAsync(memberIds, ct);

        var items = attendances
            .Select(a => a.ToResponse(members.TryGetValue(a.MemberId, out var m) ? m.FullName : null))
            .ToList();

        return new AttendanceListResponse(session.Id, validCount, items);
    }

    private static AttendanceStatus? ParseFilter(string? filter) => filter switch
    {
        null or "" or AttendanceStatusFilter.Valid => AttendanceStatus.Valid,
        AttendanceStatusFilter.Cancelled => AttendanceStatus.Cancelled,
        AttendanceStatusFilter.All => null,
        _ => AttendanceStatus.Valid,
    };
}
