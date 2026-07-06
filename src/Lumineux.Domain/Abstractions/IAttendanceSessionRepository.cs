using Lumineux.Domain.Entities;

namespace Lumineux.Domain.Abstractions;

/// <summary>Port de persistance des sessions de présence.</summary>
public interface IAttendanceSessionRepository
{
    Task<AttendanceSession?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Indique s'il existe déjà une session ouverte pour cette antenne à ce créneau (FR-003).</summary>
    Task<bool> HasOpenSessionAsync(int antennaId, DateTime meetingDate, CancellationToken ct = default);

    /// <summary>Indique si l'antenne porte au moins une session encore ouverte (feature 016, FR-005a).</summary>
    Task<bool> HasOpenSessionForAntennaAsync(int antennaId, CancellationToken ct = default);

    Task AddAsync(AttendanceSession session, CancellationToken ct = default);

    /// <summary>Liste (suivies) les sessions encore ouvertes dont la date de réunion précède le seuil (clôture auto, FR-024).</summary>
    Task<IReadOnlyList<AttendanceSession>> ListOpenBeforeAsync(DateTime startedBeforeUtc, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
