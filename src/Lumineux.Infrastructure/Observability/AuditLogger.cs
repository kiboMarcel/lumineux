using Lumineux.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Lumineux.Infrastructure.Observability;

/// <summary>
/// Journalisation structurée des opérations sensibles et des refus (FR-019/FR-020).
/// Ne consigne que des identifiants non sensibles, jamais de secret ni de donnée personnelle superflue.
/// </summary>
public sealed class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> _logger;
    private readonly ICurrentUser _currentUser;

    public AuditLogger(ILogger<AuditLogger> logger, ICurrentUser currentUser)
    {
        _logger = logger;
        _currentUser = currentUser;
    }

    public void Operation(string action, object? details = null) =>
        _logger.LogInformation(
            "AUDIT op={Action} actor={Actor} details={@Details}",
            action, _currentUser.MemberId, details);

    public void Refused(string action, string reason, object? details = null) =>
        _logger.LogWarning(
            "AUDIT refused op={Action} reason={Reason} actor={Actor} details={@Details}",
            action, reason, _currentUser.MemberId, details);
}
