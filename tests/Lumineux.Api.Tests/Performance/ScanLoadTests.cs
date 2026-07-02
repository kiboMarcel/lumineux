using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lumineux.Api.Tests.Infrastructure;
using Lumineux.Domain.Entities;
using Lumineux.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lumineux.Api.Tests.Performance;

/// <summary>
/// Vérifie l'objectif SC-006 : absorber ≥ 200 scans en moins de 2 minutes, sans doublon ni erreur.
/// Exécuté sur SQLite en mémoire (in-process) : valide le débit fonctionnel et l'absence de doublon.
/// La validation de charge représentative reste à mener sur SQL Server.
/// </summary>
public sealed class ScanLoadTests : IClassFixture<ApiTestFixture>
{
    private const int ScanCount = 200;

    private readonly ApiTestFixture _fixture;

    public ScanLoadTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Handles_200_distinct_scans_under_two_minutes()
    {
        var memberIds = SeedMembers(ScanCount);
        var (sessionId, token) = await CreateSessionWithLongLivedTokenAsync();

        var client = _fixture.CreateClient();
        var created = 0;
        var stopwatch = Stopwatch.StartNew();

        foreach (var memberId in memberIds)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"/api/v1/attendance-sessions/{sessionId}/scan")
            {
                Content = JsonContent.Create(new { token }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.IssueMemberToken(memberId));

            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            created++;
        }

        stopwatch.Stop();

        created.Should().Be(ScanCount);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMinutes(2));

        // Aucun doublon : le décompte des présences valides égale le nombre de scans.
        var bureau = _fixture.CreateClient();
        bureau.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.IssueBureauToken());
        var list = await bureau.GetAsync($"/api/v1/attendance-sessions/{sessionId}/attendances");
        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("validCount").GetInt32().Should().Be(ScanCount);
    }

    private List<int> SeedMembers(int count)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var antennaId = db.Antennas.First().Id;

        var members = Enumerable.Range(0, count)
            .Select(i => new Member { FirstName = "Load", LastName = $"Member{i}", Status = "Active", AntennaId = antennaId })
            .ToList();
        db.Members.AddRange(members);
        db.SaveChanges();

        return members.Select(m => m.Id).ToList();
    }

    private async Task<(int SessionId, string Token)> CreateSessionWithLongLivedTokenAsync()
    {
        var bureau = _fixture.CreateClient();
        bureau.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.IssueBureauToken());

        // qrStepSeconds = 120 : le jeton reste valide pendant toute la boucle de scans.
        var create = await bureau.PostAsJsonAsync(
            "/api/v1/attendance-sessions",
            new { antennaId = ApiTestFixture.SeededAntennaId, meetingDate = "2027-02-01T09:00:00Z", qrStepSeconds = 120 });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createdDoc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = createdDoc.RootElement.GetProperty("id").GetInt32();

        var qr = await bureau.GetAsync($"/api/v1/attendance-sessions/{id}/qr");
        using var qrDoc = JsonDocument.Parse(await qr.Content.ReadAsStringAsync());
        var token = qrDoc.RootElement.GetProperty("token").GetString()!;

        return (id, token);
    }
}
