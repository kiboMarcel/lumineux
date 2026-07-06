using FluentAssertions;
using Lumineux.Domain.Entities;
using Lumineux.Infrastructure.Persistence;
using Lumineux.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lumineux.Infrastructure.Tests;

/// <summary>
/// Récupération des sessions ouvertes par initiateur (feature 023) : seules les sessions Open de
/// l'initiateur demandé sont renvoyées (clôturées et sessions d'autres membres exclues).
/// </summary>
public sealed class OpenSessionsByOpenerTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 7, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public OpenSessionsByOpenerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Returns_only_open_sessions_of_the_requested_opener()
    {
        var antenna = new Antenna { Code = "A1", Label = "Antenne 1", District = 1, Status = "Active" };
        _db.Antennas.Add(antenna);
        await _db.SaveChangesAsync();

        // Membre 4 : une session ouverte + une clôturée. Membre 5 : une session ouverte.
        var openBy4 = AttendanceSession.Start(antenna.Id, Now.Date, openedByMemberId: 4, "s", 30, Now);
        var closedBy4 = AttendanceSession.Start(antenna.Id, Now.Date.AddDays(-1), 4, "s", 30, Now.AddDays(-1));
        closedBy4.Close(closedByMemberId: 4, Now);
        var openBy5 = AttendanceSession.Start(antenna.Id, Now.Date.AddDays(1), openedByMemberId: 5, "s", 30, Now);
        _db.AttendanceSessions.AddRange(openBy4, closedBy4, openBy5);
        await _db.SaveChangesAsync();

        var repo = new AttendanceSessionRepository(_db);
        var result = await repo.ListOpenByOpenerAsync(4);

        result.Should().ContainSingle().Which.Id.Should().Be(openBy4.Id);
    }

    [Fact]
    public async Task Returns_empty_when_opener_has_no_open_session()
    {
        var repo = new AttendanceSessionRepository(_db);
        (await repo.ListOpenByOpenerAsync(999)).Should().BeEmpty();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
