using FluentAssertions;
using Lumineux.Domain.Entities;
using Lumineux.Infrastructure.BackgroundJobs;
using Lumineux.Infrastructure.Persistence;
using Lumineux.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lumineux.Infrastructure.Tests;

/// <summary>
/// Non-régression de l'auto-clôture (FR-024) : l'expiration se mesure sur la durée d'ouverture
/// réelle (StartTime), pas sur MeetingDate (minuit), et l'heure de fin par défaut reste véridique
/// (jamais avant le démarrage, jamais dans le futur).
/// </summary>
public sealed class SessionAutoCloseTests : IDisposable
{
    // Réunion du jour à minuit ; il est actuellement 22:45 (scénario réel du bug).
    private static readonly DateTime Now = new(2026, 7, 5, 22, 45, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public SessionAutoCloseTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Session_started_today_is_not_expired_by_its_meeting_date()
    {
        // Démarrée à l'instant (StartTime = Now) mais MeetingDate = date du jour (minuit).
        await SeedSessionAsync(startTime: Now, meetingDate: Now.Date);
        var repository = new AttendanceSessionRepository(_db);

        // Seuil = now - 6h. Avant le correctif, MeetingDate(minuit) < seuil => faux positif.
        var threshold = Now.AddHours(-6);
        var expired = await repository.ListOpenBeforeAsync(threshold);

        expired.Should().BeEmpty("une session ouverte depuis 0 min ne doit pas être auto-clôturée");
    }

    [Fact]
    public async Task Session_open_longer_than_threshold_is_expired()
    {
        // Ouverte depuis 7h (StartTime = now - 7h) : dépasse MaxOpenHours = 6h.
        await SeedSessionAsync(startTime: Now.AddHours(-7), meetingDate: Now.Date);
        var repository = new AttendanceSessionRepository(_db);

        var expired = await repository.ListOpenBeforeAsync(Now.AddHours(-6));

        expired.Should().HaveCount(1);
    }

    [Fact]
    public void AutoClose_end_time_is_derived_from_start_never_before_start_nor_in_future()
    {
        var options = new AutoCloseOptions { DefaultDurationHours = 3 };
        var session = AttendanceSession.Start(1, Now.Date, openedByMemberId: 4, "secret", 30, nowUtc: Now.AddHours(-7));

        var endTime = SessionAutoCloseService.ComputeAutoCloseEndTime(session, options, Now);

        endTime.Should().Be(session.StartTime.AddHours(3));
        endTime.Should().BeAfter(session.StartTime);
        endTime.Should().BeOnOrBefore(Now);
        // Jamais la valeur incohérente d'avant le correctif (MeetingDate + 3h = 03:00).
        endTime.Should().NotBe(session.MeetingDate.AddHours(3));
    }

    [Fact]
    public void AutoClose_end_time_is_clamped_to_now_when_duration_exceeds_open_span()
    {
        var options = new AutoCloseOptions { DefaultDurationHours = 3 };
        // Ouverte depuis 1h seulement : start + 3h serait dans le futur => borné à now.
        var session = AttendanceSession.Start(1, Now.Date, 4, "secret", 30, nowUtc: Now.AddHours(-1));

        var endTime = SessionAutoCloseService.ComputeAutoCloseEndTime(session, options, Now);

        endTime.Should().Be(Now);
    }

    private async Task SeedSessionAsync(DateTime startTime, DateTime meetingDate)
    {
        var antenna = new Antenna { Code = "A1", Label = "Antenne 1", District = 1, Status = "Active" };
        _db.Antennas.Add(antenna);
        await _db.SaveChangesAsync();

        var session = AttendanceSession.Start(antenna.Id, meetingDate, openedByMemberId: 4, "secret", 30, startTime);
        _db.AttendanceSessions.Add(session);
        await _db.SaveChangesAsync();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
