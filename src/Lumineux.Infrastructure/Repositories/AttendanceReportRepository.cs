using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Enums;
using Lumineux.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lumineux.Infrastructure.Repositories;

/// <summary>
/// Agrégations de présence (feature 018) en **lecture seule**. Les requêtes restent simples (Where +
/// Contains + Count) pour une traduction robuste sur SQL Server comme SQLite ; la fusion par antenne
/// se fait en mémoire (volumétrie modérée). Seules les présences valides sont comptées.
/// </summary>
public sealed class AttendanceReportRepository : IAttendanceReportRepository
{
    private readonly AppDbContext _db;

    public AttendanceReportRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AntennaSummaryRow>> GetAntennaSummaryAsync(
        DateTime from, DateTime to, int? antennaId, CancellationToken ct = default)
    {
        var lower = from.Date;
        var upperExclusive = to.Date.AddDays(1); // borne haute inclusive sur la date de réunion

        // Sessions de la période (petite volumétrie) : id + antenne.
        var sessions = await _db.AttendanceSessions.AsNoTracking()
            .Where(s => s.MeetingDate >= lower && s.MeetingDate < upperExclusive
                        && (antennaId == null || s.AntennaId == antennaId))
            .Select(s => new { s.Id, s.AntennaId })
            .ToListAsync(ct);

        if (sessions.Count == 0)
        {
            return Array.Empty<AntennaSummaryRow>();
        }

        var sessionToAntenna = sessions.ToDictionary(s => s.Id, s => s.AntennaId);
        var sessionIds = sessionToAntenna.Keys.ToList();

        // Présences valides de ces sessions → session concernée (comptage par antenne en mémoire).
        var validSessionRefs = await _db.Attendances.AsNoTracking()
            .Where(a => a.Status == AttendanceStatus.Valid && sessionIds.Contains(a.SessionId))
            .Select(a => a.SessionId)
            .ToListAsync(ct);

        var sessionCountByAntenna = sessions
            .GroupBy(s => s.AntennaId)
            .ToDictionary(g => g.Key, g => g.Count());

        var validCountByAntenna = validSessionRefs
            .GroupBy(id => sessionToAntenna[id])
            .ToDictionary(g => g.Key, g => g.Count());

        var antennaIds = sessionCountByAntenna.Keys.ToList();
        var labels = await _db.Antennas.AsNoTracking()
            .Where(x => antennaIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Label, ct);

        return antennaIds
            .Select(id => new AntennaSummaryRow(
                id,
                labels.TryGetValue(id, out var label) ? label : $"#{id}",
                sessionCountByAntenna[id],
                validCountByAntenna.TryGetValue(id, out var vc) ? vc : 0))
            .OrderBy(r => r.AntennaLabel)
            .ToList();
    }

    public async Task<MemberRateData?> GetMemberRateDataAsync(
        int memberId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var member = await _db.Members.AsNoTracking()
            .Where(m => m.Id == memberId)
            .Select(m => new { m.FirstName, m.LastName, m.AntennaId })
            .FirstOrDefaultAsync(ct);

        if (member is null)
        {
            return null;
        }

        var lower = from.Date;
        var upperExclusive = to.Date.AddDays(1);
        var fullName = $"{member.FirstName} {member.LastName}".Trim();

        // Sessions éligibles = sessions de l'antenne d'origine du membre sur la période.
        var eligibleSessionCount = member.AntennaId is int origin
            ? await _db.AttendanceSessions.AsNoTracking()
                .CountAsync(s => s.AntennaId == origin && s.MeetingDate >= lower && s.MeetingDate < upperExclusive, ct)
            : 0;

        // Présences valides du membre sur des sessions de la période (toutes antennes).
        var sessionIds = await _db.AttendanceSessions.AsNoTracking()
            .Where(s => s.MeetingDate >= lower && s.MeetingDate < upperExclusive)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var validAttendanceCount = sessionIds.Count == 0
            ? 0
            : await _db.Attendances.AsNoTracking()
                .CountAsync(a => a.MemberId == memberId && a.Status == AttendanceStatus.Valid
                                 && sessionIds.Contains(a.SessionId), ct);

        return new MemberRateData(fullName, member.AntennaId, validAttendanceCount, eligibleSessionCount);
    }

    public async Task<IReadOnlyList<SessionValidCount>> GetSessionValidCountsAsync(
        DateTime from, DateTime to, int? antennaId, CancellationToken ct = default)
    {
        var lower = from.Date;
        var upperExclusive = to.Date.AddDays(1);

        // Sessions de la période (petite volumétrie) : id + date de réunion.
        var sessions = await _db.AttendanceSessions.AsNoTracking()
            .Where(s => s.MeetingDate >= lower && s.MeetingDate < upperExclusive
                        && (antennaId == null || s.AntennaId == antennaId))
            .Select(s => new { s.Id, s.MeetingDate })
            .ToListAsync(ct);

        if (sessions.Count == 0)
        {
            return Array.Empty<SessionValidCount>();
        }

        var sessionIds = sessions.Select(s => s.Id).ToList();

        // Présences valides de ces sessions → comptage par session (en mémoire).
        var validBySession = (await _db.Attendances.AsNoTracking()
                .Where(a => a.Status == AttendanceStatus.Valid && sessionIds.Contains(a.SessionId))
                .Select(a => a.SessionId)
                .ToListAsync(ct))
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        return sessions
            .Select(s => new SessionValidCount(
                s.MeetingDate,
                validBySession.TryGetValue(s.Id, out var vc) ? vc : 0))
            .ToList();
    }
}
