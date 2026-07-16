using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Sessions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;

namespace Lumineux.Application.AttendanceSessions;

/// <summary>
/// Cas d'usage : démarrage d'une session de présence par un membre du bureau (US1, FR-001..003).
/// </summary>
public sealed class StartSessionHandler
{
    private const int DefaultQrStepSeconds = 30;

    private readonly IAttendanceSessionRepository _sessions;
    private readonly IAntennaReadRepository _antennas;
    private readonly IQrTokenService _qr;
    private readonly IClock _clock;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;
    private readonly IValidator<StartSessionRequest> _validator;

    public StartSessionHandler(
        IAttendanceSessionRepository sessions,
        IAntennaReadRepository antennas,
        IQrTokenService qr,
        IClock clock,
        ICurrentUser user,
        IAuditLogger audit,
        IValidator<StartSessionRequest> validator)
    {
        _sessions = sessions;
        _antennas = antennas;
        _qr = qr;
        _clock = clock;
        _user = user;
        _audit = audit;
        _validator = validator;
    }

    public async Task<SessionResponse> HandleAsync(StartSessionRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        if (!_user.HasPermission(Permissions.ManageAttendance) || _user.MemberId is not { } memberId)
        {
            _audit.Refused("StartSession", "Droit manage_attendance manquant");
            throw new ForbiddenException("Droit requis pour gérer les présences.");
        }

        if (!await _antennas.ExistsAsync(request.AntennaId, ct))
        {
            throw new NotFoundException("Antenne introuvable.");
        }

        if (await _sessions.HasOpenSessionAsync(request.AntennaId, request.MeetingDate, ct))
        {
            _audit.Refused("StartSession", "Session déjà ouverte pour ce créneau", new { request.AntennaId });
            throw new ConflictException("Une session ouverte existe déjà pour cette antenne à ce créneau.");
        }

        var step = request.QrStepSeconds ?? DefaultQrStepSeconds;
        // Type validé en amont ; absent → défaut AntennaMeeting (feature 031).
        var sessionType = request.SessionType is null
            ? SessionType.AntennaMeeting
            : Enum.Parse<SessionType>(request.SessionType, ignoreCase: false);
        var secret = _qr.GenerateSecret();
        var session = AttendanceSession.Start(
            request.AntennaId, request.MeetingDate, memberId, secret, step, _clock.UtcNow, sessionType);

        await _sessions.AddAsync(session, ct);
        await _sessions.SaveChangesAsync(ct);

        _audit.Operation("StartSession", new { session.Id, session.AntennaId, session.SessionType });
        return session.ToResponse();
    }
}
