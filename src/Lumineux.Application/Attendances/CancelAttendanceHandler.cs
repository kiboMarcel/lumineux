using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Attendances;

/// <summary>
/// Cas d'usage : retrait/annulation d'une présence enregistrée par erreur, tant que la session
/// est ouverte (US3, FR-016). La trace est conservée (statut Cancelled).
/// </summary>
public sealed class CancelAttendanceHandler
{
    private readonly IAttendanceSessionRepository _sessions;
    private readonly IAttendanceRepository _attendances;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;

    public CancelAttendanceHandler(
        IAttendanceSessionRepository sessions,
        IAttendanceRepository attendances,
        ICurrentUser user,
        IAuditLogger audit)
    {
        _sessions = sessions;
        _attendances = attendances;
        _user = user;
        _audit = audit;
    }

    public async Task HandleAsync(int sessionId, int memberId, CancellationToken ct = default)
    {
        if (!_user.HasPermission(Permissions.ManageAttendance))
        {
            _audit.Refused("CancelAttendance", "Droit manage_attendance manquant", new { sessionId, memberId });
            throw new ForbiddenException("Droit requis pour gérer les présences.");
        }

        var session = await _sessions.GetByIdAsync(sessionId, ct)
            ?? throw new NotFoundException("Session introuvable.");

        if (!session.IsOpen)
        {
            throw new ConflictException("La réunion est terminée : annulation impossible.");
        }

        var attendance = await _attendances.GetValidByMemberAsync(session.Id, memberId, ct)
            ?? throw new NotFoundException("Présence introuvable pour ce membre.");

        attendance.Cancel();
        await _attendances.SaveChangesAsync(ct);

        _audit.Operation("CancelAttendance", new { AttendanceId = attendance.Id, SessionId = session.Id, memberId });
    }
}
