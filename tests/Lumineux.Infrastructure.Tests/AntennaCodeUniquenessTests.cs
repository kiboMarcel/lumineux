using FluentAssertions;
using Lumineux.Domain.Entities;
using Lumineux.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lumineux.Infrastructure.Tests;

/// <summary>
/// Vérifie que l'index unique sur `antennas.code` (feature 016, FR-002) rejette deux antennes de même
/// code au niveau base (défense en profondeur, Principe II).
/// </summary>
public sealed class AntennaCodeUniquenessTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public AntennaCodeUniquenessTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Second_antenna_with_same_code_is_rejected()
    {
        _db.Antennas.Add(Antenna.Create("ANT-DUP", "Antenne A", 1));
        await _db.SaveChangesAsync();

        _db.Antennas.Add(Antenna.Create("ANT-DUP", "Antenne B", 2));
        var act = () => _db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
