using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lumineux.Infrastructure.Security;

/// <summary>
/// Amorçage minimal des droits d'un compte bureau initial (F1) : accorde de façon idempotente les
/// permissions configurées (`Auth:Bootstrap`) au démarrage, pour rendre le système utilisable de bout
/// en bout avant la future feature de gestion des profils. Ne fait rien si non configuré.
/// </summary>
public sealed class PermissionBootstrapper : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<AuthOptions> _options;
    private readonly ILogger<PermissionBootstrapper> _logger;

    public PermissionBootstrapper(
        IServiceScopeFactory scopeFactory,
        IOptions<AuthOptions> options,
        ILogger<PermissionBootstrapper> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var bootstrap = _options.Value.Bootstrap;
        if (string.IsNullOrWhiteSpace(bootstrap.MemberReference) || bootstrap.Permissions.Length == 0)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IMemberAccountRepository>();
        var permissions = scope.ServiceProvider.GetRequiredService<IMemberPermissionRepository>();

        var account = await accounts.GetByLoginIdAsync(bootstrap.MemberReference, cancellationToken);
        if (account is null)
        {
            _logger.LogWarning("Amorçage des droits : membre {Reference} introuvable", bootstrap.MemberReference);
            return;
        }

        var added = false;
        foreach (var permission in bootstrap.Permissions)
        {
            if (!await permissions.HasPermissionAsync(account.MemberId, permission, cancellationToken))
            {
                await permissions.AddAsync(
                    new MemberPermission { MemberId = account.MemberId, Permission = permission }, cancellationToken);
                added = true;
            }
        }

        if (added)
        {
            await permissions.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Amorçage des droits appliqué pour {Reference}", bootstrap.MemberReference);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
