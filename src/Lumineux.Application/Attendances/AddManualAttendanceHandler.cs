using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Attendances;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;

namespace Lumineux.Application.Attendances;

/// <summary>
/// Cas d'usage : ajout manuel d'une présence par le bureau pour un membre non équipé (US3, FR-014..017).
/// </summary>
public sealed class AddManualAttendanceHandler
{
    private readonly IAttendanceSessionRepository _sessions;
    private readonly IAttendanceRepository _attendances;
    private readonly IMemberReadRepository _members;
    private readonly IClock _clock;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;
    private readonly IValidator<ManualAttendanceRequest> _validator;

    public AddManualAttendanceHandler(
        IAttendanceSessionRepository sessions,
        IAttendanceRepository attendances,
        IMemberReadRepository members,
        IClock clock,
        ICurrentUser user,
        IAuditLogger audit,
        IValidator<ManualAttendanceRequest> validator)
    {
        _sessions = sessions;
        _attendances = attendances;
        _members = members;
        _clock = clock;
        _user = user;
        _audit = audit;
        _validator = validator;
    }

    public async Task<ManualAttendanceResult> HandleAsync(int sessionId, ManualAttendanceRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        if (!_user.HasPermission(Permissions.ManageAttendance))
        {
            _audit.Refused("AddManualAttendance", "Droit manage_attendance manquant", new { sessionId, request.MemberId });
            throw new ForbiddenException("Droit requis pour gérer les présences.");
        }

        var session = await _sessions.GetByIdAsync(sessionId, ct)
            ?? throw new NotFoundException("Session introuvable.");

        if (!session.IsOpen)
        {
            throw new ConflictException("La réunion est terminée : ajout impossible.");
        }

        var member = await _members.GetByIdAsync(request.MemberId, ct)
            ?? throw new NotFoundException("Membre introuvable.");

        if (!member.IsActive)
        {
            _audit.Refused("AddManualAttendance", "Membre non actif", new { sessionId, request.MemberId });
            throw new ForbiddenException("Ce membre n'est pas actif : présence non enregistrable.");
        }

        var existing = await _attendances.GetValidByMemberAsync(session.Id, member.Id, ct);
        if (existing is not null)
        {
            return new ManualAttendanceResult(existing.ToResponse(member.FullName), AlreadyPresent: true);
        }

        var arrival = request.ArrivalTime?.ToUniversalTime() ?? _clock.UtcNow;
        var attendance = Attendance.RecordManual(session.Id, member.Id, arrival, member.AntennaId);
        await _attendances.AddAsync(attendance, ct);
        await _attendances.SaveChangesAsync(ct);

        _audit.Operation("AddManualAttendance", new { AttendanceId = attendance.Id, SessionId = session.Id, member.Id });
        return new ManualAttendanceResult(attendance.ToResponse(member.FullName), AlreadyPresent: false);
    }
}
