using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Attendances;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;

namespace Lumineux.Application.Attendances;

/// <summary>
/// Cas d'usage : synchronisation d'un lot de scans hors ligne (US2, FR-023/023a/023b).
/// Idempotent (clientOperationId), conserve l'heure réelle d'arrivée bornée par le serveur,
/// applique la règle de synchronisation post-clôture.
/// </summary>
public sealed class SyncOfflineScansHandler
{
    private readonly IAttendanceSessionRepository _sessions;
    private readonly IAttendanceRepository _attendances;
    private readonly IMemberReadRepository _members;
    private readonly IQrTokenService _qr;
    private readonly IClock _clock;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;
    private readonly IValidator<OfflineScanBatchRequest> _validator;

    public SyncOfflineScansHandler(
        IAttendanceSessionRepository sessions,
        IAttendanceRepository attendances,
        IMemberReadRepository members,
        IQrTokenService qr,
        IClock clock,
        ICurrentUser user,
        IAuditLogger audit,
        IValidator<OfflineScanBatchRequest> validator)
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

    public async Task<OfflineScanBatchResponse> HandleAsync(
        int sessionId, OfflineScanBatchRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        if (_user.MemberId is not { } memberId)
        {
            throw new ForbiddenException("Membre authentifié requis pour synchroniser.");
        }

        var session = await _sessions.GetByIdAsync(sessionId, ct)
            ?? throw new NotFoundException("Session introuvable.");

        var member = await _members.GetByIdAsync(memberId, ct)
            ?? throw new ForbiddenException("Membre inconnu.");

        if (!member.IsActive)
        {
            throw new ForbiddenException("Votre compte n'est pas actif : présence non enregistrable.");
        }

        var results = new List<OfflineScanResult>(request.Items.Count);
        foreach (var item in request.Items)
        {
            results.Add(await ProcessItemAsync(session, memberId, member.AntennaId, item, ct));
        }

        _audit.Operation("SyncOfflineScans", new { sessionId, memberId, count = request.Items.Count });
        return new OfflineScanBatchResponse(results);
    }

    private async Task<OfflineScanResult> ProcessItemAsync(
        AttendanceSession session, int memberId, int? originAntennaId, OfflineScanItem item, CancellationToken ct)
    {
        var arrival = item.ClientArrivalTime.ToUniversalTime();

        // Idempotence : déjà synchronisé (même opération) ou membre déjà présent.
        var byOperation = await _attendances.GetByClientOperationIdAsync(session.Id, item.ClientOperationId, ct);
        if (byOperation is not null)
        {
            return Result(item, OfflineScanOutcome.AlreadyPresent, attendanceId: byOperation.Id);
        }

        if (!_qr.Validate(session.QrSecret, session.QrStepSeconds, item.Token, arrival))
        {
            return Result(item, OfflineScanOutcome.Rejected, reason: "Jeton QR invalide au moment du scan.");
        }

        if (arrival < session.StartTime || arrival > _clock.UtcNow)
        {
            return Result(item, OfflineScanOutcome.Rejected, reason: "Heure d'arrivée hors de la plage de la session.");
        }

        // FR-023b : synchronisation après clôture.
        if (!session.IsOpen && session.EndTime is { } endTime && arrival >= endTime)
        {
            return Result(item, OfflineScanOutcome.Rejected, reason: "Arrivée postérieure à la clôture de la session.");
        }

        var existingValid = await _attendances.GetValidByMemberAsync(session.Id, memberId, ct);
        if (existingValid is not null)
        {
            return Result(item, OfflineScanOutcome.AlreadyPresent, attendanceId: existingValid.Id);
        }

        try
        {
            var attendance = Attendance.RecordScan(session.Id, memberId, arrival, originAntennaId, item.ClientOperationId);
            await _attendances.AddAsync(attendance, ct);
            await _attendances.SaveChangesAsync(ct);
            return Result(item, OfflineScanOutcome.Created, attendanceId: attendance.Id);
        }
        catch (ConflictException)
        {
            // Course avec un autre scan : la contrainte d'unicité a rejeté le doublon.
            var current = await _attendances.GetValidByMemberAsync(session.Id, memberId, ct);
            return Result(item, OfflineScanOutcome.AlreadyPresent, attendanceId: current?.Id);
        }
    }

    private static OfflineScanResult Result(OfflineScanItem item, string outcome, string? reason = null, int? attendanceId = null) =>
        new(item.ClientOperationId, outcome, reason, attendanceId);
}
