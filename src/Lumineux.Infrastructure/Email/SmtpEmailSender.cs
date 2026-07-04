using System.Net;
using System.Net.Mail;
using Lumineux.Domain.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lumineux.Infrastructure.Email;

/// <summary>
/// Envoi d'e-mail via SMTP (paramètres/secrets par configuration). Le corps contient le mot de passe
/// temporaire (canal de transmission légitime) mais celui-ci n'est jamais journalisé (FR-016).
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmailSendOutcome> SendInvitationAsync(
        string? toEmail, string loginId, string temporaryPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return EmailSendOutcome.NoRecipient;
        }

        try
        {
            using var client = new SmtpClient(_options.Smtp.Host, _options.Smtp.Port)
            {
                EnableSsl = _options.Smtp.UseStartTls,
                Credentials = new NetworkCredential(_options.Smtp.User, _options.Smtp.Password),
            };
            using var message = new MailMessage(_options.FromAddress, toEmail)
            {
                Subject = "Votre accès à Lumineux",
                Body = $"Bienvenue.\r\nIdentifiant de connexion : {loginId}\r\n" +
                       $"Mot de passe temporaire : {temporaryPassword}\r\n" +
                       "Vous devrez le changer à votre première connexion.",
            };

            await client.SendMailAsync(message, ct);
            _logger.LogInformation("Invitation envoyée login={LoginId}", loginId);
            return EmailSendOutcome.Sent;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Échec de l'envoi de l'invitation login={LoginId}", loginId);
            return EmailSendOutcome.Failed;
        }
    }

    public async Task<EmailSendOutcome> SendPasswordResetAsync(
        string? toEmail, string resetLink, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return EmailSendOutcome.NoRecipient;
        }

        try
        {
            using var client = new SmtpClient(_options.Smtp.Host, _options.Smtp.Port)
            {
                EnableSsl = _options.Smtp.UseStartTls,
                Credentials = new NetworkCredential(_options.Smtp.User, _options.Smtp.Password),
            };
            using var message = new MailMessage(_options.FromAddress, toEmail)
            {
                Subject = "Réinitialisation de votre mot de passe Lumineux",
                Body = "Vous avez demandé la réinitialisation de votre mot de passe.\r\n" +
                       "Cliquez sur le lien suivant pour définir un nouveau mot de passe :\r\n" +
                       resetLink + "\r\n" +
                       "Ce lien expire prochainement et ne peut être utilisé qu'une seule fois.\r\n" +
                       "Si vous n'êtes pas à l'origine de cette demande, ignorez cet e-mail.",
            };

            await client.SendMailAsync(message, ct);
            // Ne journalise JAMAIS le lien (contient le jeton en clair, FR-009/SC-004).
            _logger.LogInformation("E-mail de réinitialisation envoyé to={To}", toEmail);
            return EmailSendOutcome.Sent;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Échec de l'envoi de la réinitialisation to={To}", toEmail);
            return EmailSendOutcome.Failed;
        }
    }
}
