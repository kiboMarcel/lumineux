using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Lumineux.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lumineux.Infrastructure.Repositories;

public sealed class AttendanceSessionRepository : IAttendanceSessionRepository
{
    private readonly AppDbContext _db;

    public AttendanceSessionRepository(AppDbContext db) => _db = db;

    public Task<AttendanceSession?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.AttendanceSessions.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> HasOpenSessionAsync(int antennaId, DateTime meetingDate, CancellationToken ct = default) =>
        _db.AttendanceSessions.AnyAsync(
            x => x.AntennaId == antennaId
                 && x.Status == SessionStatus.Open
                 && x.MeetingDate == meetingDate,
            ct);

    public async Task AddAsync(AttendanceSession session, CancellationToken ct = default) =>
        await _db.AttendanceSessions.AddAsync(session, ct);

    public async Task<IReadOnlyList<AttendanceSession>> ListOpenBeforeAsync(DateTime meetingDateThreshold, CancellationToken ct = default) =>
        await _db.AttendanceSessions
            .Where(x => x.Status == SessionStatus.Open && x.MeetingDate < meetingDateThreshold)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
