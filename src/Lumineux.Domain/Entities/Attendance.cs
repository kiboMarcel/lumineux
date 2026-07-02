using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Enums;

namespace Lumineux.Domain.Entities;

/// <summary>
/// Présence d'un membre à une session. Entité riche : la création et les transitions
/// (annulation, propagation de l'heure de fin) sont portées par le domaine.
/// </summary>
public class Attendance : AbstractEntity
{
    public int SessionId { get; private set; }

    public int MemberId { get; private set; }

    public DateTime ArrivalTime { get; private set; }

    public DateTime? EndTime { get; private set; }

    public AttendanceSource Source { get; private set; }

    public AttendanceStatus Status { get; private set; }

    /// <summary>Instantané de l'antenne d'origine du membre au moment de la réunion (FR-011).</summary>
    public int? OriginAntennaId { get; private set; }

    /// <summary>Identifiant d'idempotence des scans hors ligne (FR-023a).</summary>
    public string? ClientOperationId { get; private set; }

    // Requis par EF Core.
    private Attendance() { }

    /// <summary>Enregistre une présence par scan (US2) ou synchronisation hors ligne.</summary>
    public static Attendance RecordScan(
        int sessionId,
        int memberId,
        DateTime arrivalTimeUtc,
        int? originAntennaId,
        string? clientOperationId = null) =>
        Create(sessionId, memberId, arrivalTimeUtc, originAntennaId, AttendanceSource.QrScan, clientOperationId);

    /// <summary>Enregistre une présence ajoutée manuellement par le bureau (US3).</summary>
    public static Attendance RecordManual(
        int sessionId,
        int memberId,
        DateTime arrivalTimeUtc,
        int? originAntennaId) =>
        Create(sessionId, memberId, arrivalTimeUtc, originAntennaId, AttendanceSource.Manual, null);

    /// <summary>Annule la présence (trace conservée, FR-016).</summary>
    public void Cancel()
    {
        if (Status == AttendanceStatus.Cancelled)
        {
            throw new ConflictException("La présence est déjà annulée.");
        }

        Status = AttendanceStatus.Cancelled;
    }

    /// <summary>Applique l'heure de fin héritée de la clôture de session (US4, FR-006).</summary>
    public void ApplyEndTime(DateTime endTimeUtc) => EndTime = endTimeUtc;

    private static Attendance Create(
        int sessionId,
        int memberId,
        DateTime arrivalTimeUtc,
        int? originAntennaId,
        AttendanceSource source,
        string? clientOperationId)
    {
        if (sessionId <= 0)
        {
            throw new DomainException("La session de la présence est invalide.");
        }

        if (memberId <= 0)
        {
            throw new DomainException("Le membre de la présence est invalide.");
        }

        return new Attendance
        {
            SessionId = sessionId,
            MemberId = memberId,
            ArrivalTime = arrivalTimeUtc,
            Source = source,
            Status = AttendanceStatus.Valid,
            OriginAntennaId = originAntennaId,
            ClientOperationId = clientOperationId,
        };
    }
}
