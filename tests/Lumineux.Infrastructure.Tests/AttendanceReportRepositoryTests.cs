using FluentAssertions;
using Lumineux.Domain.Entities;
using Lumineux.Infrastructure.Persistence;
using Lumineux.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lumineux.Infrastructure.Tests;

/// <summary>
/// Agrégations de présence (feature 018) sur base réelle (SQLite) : synthèse par antenne, exclusion des
/// présences annulées, dénominateur du taux = sessions de l'antenne d'origine.
/// </summary>
public sealed class AttendanceReportRepositoryTests : IDisposable
{
    private static readonly DateTime Jun1 = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Jun30 = new(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private int _a1, _a2, _m1, _m2, _m3;

    public AttendanceReportRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        Seed();
    }

    [Fact]
    public async Task Antenna_summary_counts_sessions_and_valid_attendances_excluding_cancelled()
    {
        var repo = new AttendanceReportRepository(_db);

        var rows = await repo.GetAntennaSummaryAsync(Jun1, Jun30, antennaId: null);

        // A1 : 2 sessions (juin), présences valides = S1(M1,M2) + S2(M1) = 3 (M2 annulé exclu).
        var a1 = rows.Single(r => r.AntennaId == _a1);
        a1.SessionCount.Should().Be(2);
        a1.ValidAttendanceCount.Should().Be(3);
        // A2 : 1 session, 1 présence valide.
        var a2 = rows.Single(r => r.AntennaId == _a2);
        a2.SessionCount.Should().Be(1);
        a2.ValidAttendanceCount.Should().Be(1);
    }

    [Fact]
    public async Task Antenna_summary_can_filter_by_antenna()
    {
        var repo = new AttendanceReportRepository(_db);
        var rows = await repo.GetAntennaSummaryAsync(Jun1, Jun30, antennaId: _a2);
        rows.Should().ContainSingle().Which.AntennaId.Should().Be(_a2);
    }

    [Fact]
    public async Task Empty_period_returns_no_rows()
    {
        var repo = new AttendanceReportRepository(_db);
        var rows = await repo.GetAntennaSummaryAsync(new DateTime(2020, 1, 1), new DateTime(2020, 1, 31), null);
        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Member_rate_denominator_is_home_antenna_sessions()
    {
        var repo = new AttendanceReportRepository(_db);

        // M1 (origine A1) : éligibles = sessions A1 de juin = 2 ; présences valides (toutes antennes) = 3.
        var m1 = await repo.GetMemberRateDataAsync(_m1, Jun1, Jun30);
        m1!.EligibleSessionCount.Should().Be(2);
        m1.ValidAttendanceCount.Should().Be(3);
        m1.OriginAntennaId.Should().Be(_a1);
    }

    [Fact]
    public async Task Member_without_attendance_yields_zero_valid()
    {
        var repo = new AttendanceReportRepository(_db);
        var m3 = await repo.GetMemberRateDataAsync(_m3, Jun1, Jun30);
        m3!.ValidAttendanceCount.Should().Be(0);
        m3.EligibleSessionCount.Should().Be(2); // A1 a 2 sessions en juin
    }

    [Fact]
    public async Task Unknown_member_returns_null()
    {
        var repo = new AttendanceReportRepository(_db);
        (await repo.GetMemberRateDataAsync(999999, Jun1, Jun30)).Should().BeNull();
    }

    private void Seed()
    {
        var a1 = new Antenna { Code = "A1", Label = "Antenne 1", District = 1, Status = "Active" };
        var a2 = new Antenna { Code = "A2", Label = "Antenne 2", District = 1, Status = "Active" };
        _db.Antennas.AddRange(a1, a2);
        _db.SaveChanges();
        _a1 = a1.Id; _a2 = a2.Id;

        var m1 = NewMember("M1", a1.Id);
        var m2 = NewMember("M2", a2.Id);
        var m3 = NewMember("M3", a1.Id);
        _db.Members.AddRange(m1, m2, m3);
        _db.SaveChanges();
        _m1 = m1.Id; _m2 = m2.Id; _m3 = m3.Id;

        var s1 = Session(a1.Id, new DateTime(2026, 6, 10));
        var s2 = Session(a1.Id, new DateTime(2026, 6, 20));
        var s3 = Session(a2.Id, new DateTime(2026, 6, 15));
        var s4 = Session(a1.Id, new DateTime(2026, 5, 1)); // hors juin
        _db.AttendanceSessions.AddRange(s1, s2, s3, s4);
        _db.SaveChanges();

        _db.Attendances.AddRange(
            Attendance.RecordScan(s1.Id, m1.Id, s1.MeetingDate, a1.Id),
            Attendance.RecordScan(s1.Id, m2.Id, s1.MeetingDate, a2.Id),
            Attendance.RecordScan(s2.Id, m1.Id, s2.MeetingDate, a1.Id),
            Attendance.RecordScan(s3.Id, m1.Id, s3.MeetingDate, a1.Id),
            Attendance.RecordScan(s4.Id, m1.Id, s4.MeetingDate, a1.Id)); // hors juin
        _db.SaveChanges();

        // M2 en S2 : présence ANNULÉE (ne doit pas compter).
        var cancelled = Attendance.RecordScan(s2.Id, m2.Id, s2.MeetingDate, a2.Id);
        cancelled.Cancel();
        _db.Attendances.Add(cancelled);
        _db.SaveChanges();
    }

    private static Member NewMember(string reference, int antennaId) => new()
    {
        Reference = reference,
        EntryDate = Jun1,
        Gender = "F",
        LastName = "Test",
        FirstName = reference,
        Status = "Active",
        AntennaId = antennaId,
    };

    private static AttendanceSession Session(int antennaId, DateTime meetingDate) =>
        AttendanceSession.Start(antennaId, meetingDate, openedByMemberId: 1, "secret", 30, meetingDate);

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
