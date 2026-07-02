using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lumineux.Infrastructure.BackgroundJobs;

/// <summary>
/// Clôture automatiquement les sessions restées ouvertes au-delà du délai configuré (FR-024),
/// en appliquant une heure de fin par défaut et en la propageant aux présences valides.
/// </summary>
public sealed class SessionAutoCloseService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<AutoCloseOptions> _options;
    private readonly ILogger<SessionAutoCloseService> _logger;

    public SessionAutoCloseService(
        IServiceScopeFactory scopeFactory,
        IOptions<AutoCloseOptions> options,
        ILogger<SessionAutoCloseService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        if (!options.Enabled)
        {
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(30, options.PollingIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                await CloseExpiredSessionsAsync(options, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Échec de la clôture automatique des sessions");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CloseExpiredSessionsAsync(AutoCloseOptions options, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sessions = scope.ServiceProvider.GetRequiredService<IAttendanceSessionRepository>();
        var attendances = scope.ServiceProvider.GetRequiredService<IAttendanceRepository>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var audit = scope.ServiceProvider.GetRequiredService<IAuditLogger>();

        var now = clock.UtcNow;
        var threshold = now.AddHours(-options.MaxOpenHours);
        var expired = await sessions.ListOpenBeforeAsync(threshold, ct);

        foreach (var session in expired)
        {
            var endTime = session.MeetingDate.AddHours(options.DefaultDurationHours);
            session.AutoClose(endTime);

            foreach (var attendance in await attendances.GetValidBySessionForUpdateAsync(session.Id, ct))
            {
                attendance.ApplyEndTime(endTime);
            }

            audit.Operation("AutoCloseSession", new { session.Id, session.AntennaId });
        }

        if (expired.Count > 0)
        {
            await sessions.SaveChangesAsync(ct);
            _logger.LogInformation("Clôture automatique de {Count} session(s)", expired.Count);
        }
    }
}
