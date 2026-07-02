using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Sessions;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.AttendanceSessions;

/// <summary>Cas d'usage : consultation d'une session (US1).</summary>
public sealed class GetSessionHandler
{
    private readonly IAttendanceSessionRepository _sessions;
    private readonly ICurrentUser _user;

    public GetSessionHandler(IAttendanceSessionRepository sessions, ICurrentUser user)
    {
        _sessions = sessions;
        _user = user;
    }

    public async Task<SessionResponse> HandleAsync(int sessionId, CancellationToken ct = default)
    {
        if (!_user.HasPermission(Permissions.ManageAttendance))
        {
            throw new ForbiddenException("Droit requis pour gérer les présences.");
        }

        var session = await _sessions.GetByIdAsync(sessionId, ct)
            ?? throw new NotFoundException("Session introuvable.");

        return session.ToResponse();
    }
}
