using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Lumineux.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lumineux.Infrastructure.Repositories;

public sealed class AttendanceRepository : IAttendanceRepository
{
    private readonly AppDbContext _db;

    public AttendanceRepository(AppDbContext db) => _db = db;

    public Task<Attendance?> GetValidByMemberAsync(int sessionId, int memberId, CancellationToken ct = default) =>
        _db.Attendances.FirstOrDefaultAsync(
            x => x.SessionId == sessionId && x.MemberId == memberId && x.Status == AttendanceStatus.Valid, ct);

    public async Task<IReadOnlyList<Attendance>> ListBySessionAsync(int sessionId, AttendanceStatus? status, CancellationToken ct = default)
    {
        var query = _db.Attendances.AsNoTracking().Where(x => x.SessionId == sessionId);
        if (status is { } value)
        {
            query = query.Where(x => x.Status == value);
        }

        return await query.OrderBy(x => x.ArrivalTime).ToListAsync(ct);
    }

    public Task<int> CountValidBySessionAsync(int sessionId, CancellationToken ct = default) =>
        _db.Attendances.CountAsync(x => x.SessionId == sessionId && x.Status == AttendanceStatus.Valid, ct);

    public async Task<IReadOnlyList<Attendance>> GetValidBySessionForUpdateAsync(int sessionId, CancellationToken ct = default) =>
        await _db.Attendances
            .Where(x => x.SessionId == sessionId && x.Status == AttendanceStatus.Valid)
            .ToListAsync(ct);

    public Task<Attendance?> GetByClientOperationIdAsync(int sessionId, string clientOperationId, CancellationToken ct = default) =>
        _db.Attendances.FirstOrDefaultAsync(
            x => x.SessionId == sessionId && x.ClientOperationId == clientOperationId, ct);

    public async Task AddAsync(Attendance attendance, CancellationToken ct = default) =>
        await _db.Attendances.AddAsync(attendance, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException("Présence déjà enregistrée pour cette session.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }
}
