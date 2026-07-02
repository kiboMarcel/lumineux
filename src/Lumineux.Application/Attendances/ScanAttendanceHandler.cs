using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Attendances;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;

namespace Lumineux.Application.Attendances;

/// <summary>
/// Cas d'usage : enregistrement d'une présence par scan du QR (US2, FR-008..013).
/// Heure d'arrivée = heure serveur ; anti-doublon ; jeton rotatif ; membre actif requis.
/// </summary>
public sealed class ScanAttendanceHandler
{
    private readonly IAttendanceSessionRepository _sessions;
    private readonly IAttendanceRepository _attendances;
    private readonly IMemberReadRepository _members;
    private readonly IQrTokenService _qr;
    private readonly IClock _clock;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;
    private readonly IValidator<ScanRequest> _validator;

    public ScanAttendanceHandler(
        IAttendanceSessionRepository sessions,
        IAttendanceRepository attendances,
        IMemberReadRepository members,
        IQrTokenService qr,
        IClock clock,
        ICurrentUser user,
        IAuditLogger audit,
        IValidator<ScanRequest> validator)
    {
        _sessions = sessions;
        _attendances = attendances;
        _members = members;
        _qr = qr;
        _clock = clock;
        _user = user;
        _audit = audit;
        _validator = validator;
    }

    public async Task<ScanResult> HandleAsync(int sessionId, ScanRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        if (_user.MemberId is not { } memberId)
        {
            throw new ForbiddenException("Membre authentifié requis pour scanner.");
        }

        var session = await _sessions.GetByIdAsync(sessionId, ct)
            ?? throw new NotFoundException("Session introuvable.");

        if (!session.IsOpen)
        {
            _audit.Refused("Scan", "Session clôturée", new { sessionId, memberId });
            throw new ConflictException("La réunion est terminée : enregistrement impossible.");
        }

        var now = _clock.UtcNow;
        if (!_qr.Validate(session.QrSecret, session.QrStepSeconds, request.Token, now))
        {
            _audit.Refused("Scan", "Jeton QR invalide ou périmé", new { sessionId, memberId });
            throw new GoneException("Code QR expiré : scannez le code affiché actuellement.");
        }

        var member = await _members.GetByIdAsync(memberId, ct)
            ?? throw new ForbiddenException("Membre inconnu.");

        if (!member.IsActive)
        {
            _audit.Refused("Scan", "Membre non actif", new { sessionId, memberId });
            throw new ForbiddenException("Votre compte n'est pas actif : présence non enregistrable.");
        }

        var existing = await _attendances.GetValidByMemberAsync(session.Id, memberId, ct);
        if (existing is not null)
        {
            return new ScanResult(existing.ToResponse(member.FullName), AlreadyPresent: true);
        }

        var attendance = Attendance.RecordScan(session.Id, memberId, now, member.AntennaId);
        await _attendances.AddAsync(attendance, ct);
        await _attendances.SaveChangesAsync(ct);

        _audit.Operation("Scan", new { AttendanceId = attendance.Id, SessionId = session.Id, memberId });
        return new ScanResult(attendance.ToResponse(member.FullName), AlreadyPresent: false);
    }
}
