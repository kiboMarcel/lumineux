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

    /// <summary>
    /// Envoie le lien de réinitialisation de mot de passe (feature 006). Le lien contient le jeton
    /// en clair ; il n'est jamais journalisé. Retourne <see cref="EmailSendOutcome.NoRecipient"/> si
    /// aucun destinataire, <see cref="EmailSendOutcome.Failed"/> en cas d'échec (la réponse API reste
    /// générique — FR-011).
    /// </summary>
    Task<EmailSendOutcome> SendPasswordResetAsync(
        string? toEmail, string resetLink, CancellationToken ct = default);
}
