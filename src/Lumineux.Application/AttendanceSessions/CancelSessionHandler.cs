using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Sessions;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.AttendanceSessions;

/// <summary>
/// Cas d'usage : annulation d'une session **ouverte et vide** créée par erreur (feature 028).
/// Autorisé uniquement si le décompte de présences valides est **0**, re-vérifié dans une
/// **transaction sérialisable** pour empêcher toute perte due à un ajout concurrent (FR-004/SC-003).
/// La session passe à l'état terminal « annulée » (conservée, tracée) ; aucune présence n'est touchée.
/// </summary>
public sealed class CancelSessionHandler
{
    private readonly IAttendanceSessionRepository _sessions;
    private readonly IAttendanceRepository _attendances;
    private readonly IClock _clock;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;

    public CancelSessionHandler(
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
            _audit.Refused("CancelSession", "Droit manage_attendance manquant", new { sessionId });
            throw new ForbiddenException("Droit requis pour gérer les présences.");
        }

        var session = await _sessions.GetByIdAsync(sessionId, ct)
            ?? throw new NotFoundException("Session introuvable.");

        if (!session.IsOpen)
        {
            _audit.Refused("CancelSession", "Session non ouverte", new { sessionId });
            throw new ConflictException("La session n'est pas ouverte : annulation impossible.");
        }

        // Contrôle « vide » + bascule d'état dans une transaction sérialisable : un scan concurrent ne
        // peut pas insérer une présence valide pendant l'annulation (FR-004/SC-003).
        return await _sessions.ExecuteInSerializableTransactionAsync(async innerCt =>
        {
            var validCount = await _attendances.CountValidBySessionAsync(session.Id, innerCt);
            if (validCount > 0)
            {
                _audit.Refused("CancelSession", "Session contient des présences", new { sessionId });
                throw new ConflictException("La session contient des présences et ne peut pas être annulée.");
            }

            session.Cancel(memberId, _clock.UtcNow); // re-garde « ouverte » côté domaine
            await _sessions.SaveChangesAsync(innerCt);

            _audit.Operation("CancelSession", new { session.Id, session.AntennaId });
            return session.ToResponse(0);
        }, ct);
    }
}
