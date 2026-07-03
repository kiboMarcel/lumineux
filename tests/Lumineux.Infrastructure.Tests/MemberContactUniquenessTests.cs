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
/// Vérifie l'unicité filtrée d'une coordonnée (e-mail) parmi les membres actifs au niveau base
/// (FR-008), traduite en ConflictException par le repository.
/// </summary>
public sealed class MemberContactUniquenessTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 7, 3, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public MemberContactUniquenessTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Two_active_members_with_same_email_are_rejected()
    {
        var antenna = new Antenna { Code = "A1", Label = "Antenne 1", District = 1, Status = "Active" };
        _db.Antennas.Add(antenna);
        await _db.SaveChangesAsync();

        var repository = new MemberRepository(_db);

        var first = Member.Create("LUM-2026-00001", Now, "Doe", "Jane", "F", antenna.Id);
        first.Email = "shared@example.com";
        await repository.AddAsync(first);
        await repository.SaveChangesAsync();

        var second = Member.Create("LUM-2026-00002", Now, "Roe", "Jane", "F", antenna.Id);
        second.Email = "shared@example.com";
        await repository.AddAsync(second);
        var act = () => repository.SaveChangesAsync();

        await act.Should().ThrowAsync<ConflictException>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
