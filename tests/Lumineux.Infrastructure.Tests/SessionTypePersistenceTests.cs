using FluentAssertions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Lumineux.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lumineux.Infrastructure.Tests;

/// <summary>
/// Feature 031 — la colonne <c>session_type</c> (enum persisté en chaîne) est écrite et relue
/// fidèlement, et le défaut <c>AntennaMeeting</c> s'applique à une session démarrée sans type.
/// </summary>
public sealed class SessionTypePersistenceTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 7, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public SessionTypePersistenceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    private async Task<int> SeedAntennaAsync()
    {
        var antenna = new Antenna { Code = "A1", Label = "Antenne 1", District = 1, Status = "Active" };
        _db.Antennas.Add(antenna);
        await _db.SaveChangesAsync();
        return antenna.Id;
    }

    private async Task<int> PersistAsync(AttendanceSession session)
    {
        _db.AttendanceSessions.Add(session);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        return session.Id;
    }

    [Fact]
    public async Task Default_session_persists_as_antenna_meeting()
    {
        var antennaId = await SeedAntennaAsync();
        var id = await PersistAsync(AttendanceSession.Start(antennaId, Now.Date, 4, "s", 30, Now));

        var reloaded = await _db.AttendanceSessions.SingleAsync(x => x.Id == id);
        reloaded.SessionType.Should().Be(SessionType.AntennaMeeting);
    }

    [Fact]
    public async Task Teaching_session_round_trips()
    {
        var antennaId = await SeedAntennaAsync();
        var id = await PersistAsync(AttendanceSession.Start(antennaId, Now.Date, 4, "s", 30, Now, SessionType.Teaching));

        var reloaded = await _db.AttendanceSessions.SingleAsync(x => x.Id == id);
        reloaded.SessionType.Should().Be(SessionType.Teaching);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
