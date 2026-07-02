using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Sessions;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.AttendanceSessions;

/// <summary>
/// Cas d'usage : clôture d'une session par le bureau (US4, FR-005..007). L'heure de clôture devient
/// l'heure de fin de réunion, propagée à toutes les présences valides dans une même sauvegarde.
/// </summary>
public sealed class CloseSessionHandler
{
    private readonly IAttendanceSessionRepository _sessions;
    private readonly IAttendanceRepository _attendances;
    private readonly IClock _clock;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;

    public CloseSessionHandler(
        IAttendanceSessionRepository sessions,
        IAttendanceRepository attendances,
        IClock clock,
        ICurrentUser user,
        IAuditLogger audit)
    {
        _sessions = sessions;
        _attendances = attendances;
        _clock = clock;
        _user = user;
        _audit = audit;
    }

    public async Task<SessionResponse> HandleAsync(int sessionId, CancellationToken ct = default)
    {
        if (!_user.HasPermission(Permissions.ManageAttendance) || _user.MemberId is not { } memberId)
        {
            _audit.Refused("CloseSession", "Droit manage_attendance manquant", new { sessionId });
            throw new ForbiddenException("Droit requis pour gérer les présences.");
        }

        var session = await _sessions.GetByIdAsync(sessionId, ct)
            ?? throw new NotFoundException("Session introuvable.");

        var now = _clock.UtcNow;
        session.Close(memberId, now); // lève ConflictException si déjà clôturée

        var attendances = await _attendances.GetValidBySessionForUpdateAsync(session.Id, ct);
        foreach (var attendance in attendances)
        {
            attendance.ApplyEndTime(now);
        }

        // Session et présences partagent le même DbContext : une seule sauvegarde = une transaction.
        await _sessions.SaveChangesAsync(ct);

        _audit.Operation("CloseSession", new { session.Id, session.AntennaId, count = attendances.Count });
        return session.ToResponse(attendances.Count);
    }
}
