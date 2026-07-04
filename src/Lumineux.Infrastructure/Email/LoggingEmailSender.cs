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

    public Task<EmailSendOutcome> SendPasswordResetAsync(
        string? toEmail, string resetLink, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return Task.FromResult(EmailSendOutcome.NoRecipient);
        }

        // Ne journalise JAMAIS le lien (il contient le jeton en clair, FR-009/SC-004).
        _logger.LogInformation("Réinitialisation de mot de passe (dev) préparée to={To}", toEmail);
        return Task.FromResult(EmailSendOutcome.Sent);
    }
}
