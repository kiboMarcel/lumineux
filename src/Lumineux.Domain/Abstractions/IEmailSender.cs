namespace Lumineux.Domain.Abstractions;

/// <summary>Résultat d'une tentative d'envoi d'e-mail.</summary>
public enum EmailSendOutcome
{
    Sent = 0,
    NoRecipient = 1,
    Failed = 2,
}

/// <summary>
/// Envoi de l'e-mail d'invitation aux nouveaux membres (FR-011). Ne journalise jamais le mot de
/// passe. Retourne un résultat exploitable pour décider du repli remise-bureau.
/// </summary>
public interface IEmailSender
{
    Task<EmailSendOutcome> SendInvitationAsync(
        string? toEmail, string loginId, string temporaryPassword, CancellationToken ct = default);
}
