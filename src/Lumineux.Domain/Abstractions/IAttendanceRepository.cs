using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;

namespace Lumineux.Domain.Abstractions;

/// <summary>Port de persistance des présences.</summary>
public interface IAttendanceRepository
{
    /// <summary>Retourne la présence valide d'un membre pour une session, si elle existe (anti-doublon).</summary>
    Task<Attendance?> GetValidByMemberAsync(int sessionId, int memberId, CancellationToken ct = default);

    /// <summary>Liste les présences d'une session, filtrées par statut (null = toutes), triées par arrivée.</summary>
    Task<IReadOnlyList<Attendance>> ListBySessionAsync(int sessionId, AttendanceStatus? status, CancellationToken ct = default);

    /// <summary>Nombre de présences valides d'une session (décompte courant, FR-021).</summary>
    Task<int> CountValidBySessionAsync(int sessionId, CancellationToken ct = default);

    /// <summary>Charge (suivies) les présences valides d'une session pour mise à jour (propagation de l'heure de fin, FR-006).</summary>
    Task<IReadOnlyList<Attendance>> GetValidBySessionForUpdateAsync(int sessionId, CancellationToken ct = default);

    /// <summary>Retourne la présence associée à un identifiant d'opération client (idempotence hors ligne).</summary>
    Task<Attendance?> GetByClientOperationIdAsync(int sessionId, string clientOperationId, CancellationToken ct = default);

    Task AddAsync(Attendance attendance, CancellationToken ct = default);

    /// <summary>
    /// Persiste les changements. Une violation de contrainte d'unicité est traduite en
    /// <see cref="Lumineux.Domain.Abstractions.ConflictException"/> (« déjà présent »).
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
