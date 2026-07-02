using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Sessions;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.AttendanceSessions;

/// <summary>Cas d'usage : récupération du jeton QR courant à afficher (US1, FR-013).</summary>
public sealed class GetCurrentQrTokenHandler
{
    private readonly IAttendanceSessionRepository _sessions;
    private readonly IQrTokenService _qr;
    private readonly IClock _clock;
    private readonly ICurrentUser _user;

    public GetCurrentQrTokenHandler(
        IAttendanceSessionRepository sessions,
        IQrTokenService qr,
        IClock clock,
        ICurrentUser user)
    {
        _sessions = sessions;
        _qr = qr;
        _clock = clock;
        _user = user;
    }

    public async Task<QrTokenResponse> HandleAsync(int sessionId, CancellationToken ct = default)
    {
        if (!_user.HasPermission(Permissions.ManageAttendance))
        {
            throw new ForbiddenException("Droit requis pour gérer les présences.");
        }

        var session = await _sessions.GetByIdAsync(sessionId, ct)
            ?? throw new NotFoundException("Session introuvable.");

        if (!session.IsOpen)
        {
            throw new ConflictException("La session est clôturée : plus de code QR disponible.");
        }

        var token = _qr.GetCurrentToken(session.QrSecret, session.QrStepSeconds, _clock.UtcNow);
        return new QrTokenResponse(token.Token, token.StepSeconds, token.ExpiresAt);
    }
}
