using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Enums;

namespace Lumineux.Domain.Entities;

/// <summary>
/// Session de présence : une réunion tenue dans une antenne à une date/heure donnée.
/// Entité riche : les invariants et transitions d'état sont portés par le domaine.
/// </summary>
public class AttendanceSession : AbstractEntity
{
    public const int MinQrStepSeconds = 10;
    public const int MaxQrStepSeconds = 120;

    public int AntennaId { get; private set; }

    public DateTime MeetingDate { get; private set; }

    public DateTime StartTime { get; private set; }

    public DateTime? EndTime { get; private set; }

    public SessionStatus Status { get; private set; }

    public int OpenedByMemberId { get; private set; }

    public int? ClosedByMemberId { get; private set; }

    /// <summary>Secret de dérivation du jeton QR rotatif — jamais exposé aux clients.</summary>
    public string QrSecret { get; private set; } = default!;

    public int QrStepSeconds { get; private set; }

    // Requis par EF Core.
    private AttendanceSession() { }

    public bool IsOpen => Status == SessionStatus.Open;

    /// <summary>Démarre une nouvelle session ouverte (heure serveur).</summary>
    public static AttendanceSession Start(
        int antennaId,
        DateTime meetingDate,
        int openedByMemberId,
        string qrSecret,
        int qrStepSeconds,
        DateTime nowUtc)
    {
        if (antennaId <= 0)
        {
            throw new DomainException("L'antenne de la session est invalide.");
        }

        if (openedByMemberId <= 0)
        {
            throw new DomainException("Le membre initiateur est invalide.");
        }

        if (string.IsNullOrWhiteSpace(qrSecret))
        {
            throw new DomainException("Le secret du code QR est requis.");
        }

        if (qrStepSeconds is < MinQrStepSeconds or > MaxQrStepSeconds)
        {
            throw new DomainException(
                $"Le pas de rotation du QR doit être compris entre {MinQrStepSeconds} et {MaxQrStepSeconds} secondes.");
        }

        return new AttendanceSession
        {
            AntennaId = antennaId,
            MeetingDate = meetingDate,
            StartTime = nowUtc,
            Status = SessionStatus.Open,
            OpenedByMemberId = openedByMemberId,
            QrSecret = qrSecret,
            QrStepSeconds = qrStepSeconds,
        };
    }

    /// <summary>Clôture la session et fixe l'heure de fin (heure serveur).</summary>
    public void Close(int closedByMemberId, DateTime nowUtc)
    {
        if (Status == SessionStatus.Closed)
        {
            throw new ConflictException("La session est déjà clôturée.");
        }

        Status = SessionStatus.Closed;
        EndTime = nowUtc;
        ClosedByMemberId = closedByMemberId;
    }

    /// <summary>
    /// Clôture automatique de secours par le système (FR-024) : idempotente, sans membre
    /// clôturant, avec une heure de fin par défaut.
    /// </summary>
    public void AutoClose(DateTime endTimeUtc)
    {
        if (Status == SessionStatus.Closed)
        {
            return;
        }

        Status = SessionStatus.Closed;
        EndTime = endTimeUtc;
        ClosedByMemberId = null;
    }
}
