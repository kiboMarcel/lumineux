using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Sessions;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.AttendanceSessions;

/// <summary>
/// Cas d'usage : récupération des sessions de présence encore **ouvertes** démarrées par l'utilisateur
/// courant (feature 023), afin de permettre la **reprise** côté console après une navigation
/// accidentelle. Lecture seule ; l'identité provient du jeton (jamais d'un paramètre client).
/// </summary>
public sealed class ListMyOpenSessionsHandler
{
    private readonly IAttendanceSessionRepository _sessions;
    private readonly ICurrentUser _user;

    public ListMyOpenSessionsHandler(IAttendanceSessionRepository sessions, ICurrentUser user)
    {
        _sessions = sessions;
        _user = user;
    }

    public async Task<IReadOnlyList<SessionResponse>> HandleAsync(CancellationToken ct = default)
    {
        if (!_user.HasPermission(Permissions.ManageAttendance))
        {
            throw new ForbiddenException("Droit requis pour gérer les présences.");
        }

        // Identité par le jeton : sans membre, aucune session à reprendre.
        if (_user.MemberId is not { } memberId)
        {
            return Array.Empty<SessionResponse>();
        }

        var sessions = await _sessions.ListOpenByOpenerAsync(memberId, ct);
        return sessions.Select(s => s.ToResponse()).ToList();
    }
}
