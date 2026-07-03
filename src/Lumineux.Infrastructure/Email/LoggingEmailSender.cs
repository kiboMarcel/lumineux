using Lumineux.Domain.Abstractions;
using Microsoft.Extensions.Logging;

namespace Lumineux.Infrastructure.Email;

/// <summary>
/// Envoi d'e-mail de développement : journalise l'intention d'envoi (sans mot de passe) et
/// simule un succès si un destinataire est fourni.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task<EmailSendOutcome> SendInvitationAsync(
        string? toEmail, string loginId, string temporaryPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return Task.FromResult(EmailSendOutcome.NoRecipient);
        }

        // Ne journalise JAMAIS le mot de passe temporaire (FR-016).
        _logger.LogInformation("Invitation e-mail (dev) préparée login={LoginId} to={To}", loginId, toEmail);
        return Task.FromResult(EmailSendOutcome.Sent);
    }
}
