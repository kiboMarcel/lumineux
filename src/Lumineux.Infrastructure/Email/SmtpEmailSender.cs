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
}
