using FluentAssertions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Infrastructure.Persistence;
using Lumineux.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lumineux.Infrastructure.Tests;

/// <summary>
/// Vérifie que la contrainte d'unicité filtrée (session, membre) sur les présences valides
/// empêche les doublons au niveau base (FR-010, SC-003), traduite en ConflictException.
/// </summary>
public sealed class AttendanceUniquenessTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 7, 5, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public AttendanceUniquenessTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Second_valid_attendance_for_same_member_and_session_is_rejected()
    {
        var (sessionId, memberId, antennaId) = await SeedAsync();
        var repository = new AttendanceRepository(_db);

        await repository.AddAsync(Attendance.RecordScan(sessionId, memberId, Now, antennaId));
        await repository.SaveChangesAsync();

        await repository.AddAsync(Attendance.RecordScan(sessionId, memberId, Now.AddMinutes(1), antennaId));
        var act = () => repository.SaveChangesAsync();

        await act.Should().ThrowAsync<ConflictException>();
    }

    private async Task<(int SessionId, int MemberId, int AntennaId)> SeedAsync()
    {
        var antenna = new Antenna { Code = "A1", Label = "Antenne 1", District = 1, Status = "Active" };
        _db.Antennas.Add(antenna);
        await _db.SaveChangesAsync();

        var member = new Member { LastName = "Doe", FirstName = "Jane", Status = "Active", AntennaId = antenna.Id };
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var session = AttendanceSession.Start(antenna.Id, Now.Date, member.Id, "secret", 30, Now);
        _db.AttendanceSessions.Add(session);
        await _db.SaveChangesAsync();

        return (session.Id, member.Id, antenna.Id);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
