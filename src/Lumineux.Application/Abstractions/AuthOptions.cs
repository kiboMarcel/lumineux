namespace Lumineux.Application.Abstractions;

/// <summary>Paramètres d'authentification (section "Auth").</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public int AccessTokenMinutes { get; set; } = 60;

    public int MaxFailedAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    public int PasswordMinLength { get; set; } = 8;

    /// <summary>Durée de vie d'un jeton de réinitialisation de mot de passe, en minutes (feature 006, FR-004).</summary>
    public int PasswordResetMinutes { get; set; } = 30;

    /// <summary>
    /// Base d'URL de la SPA vers laquelle pointe le lien de réinitialisation (feature 006). Le handler
    /// construit <c>{base}?token={jeton}</c>.
    /// </summary>
    public string PasswordResetUrlBase { get; set; } = "https://localhost:4200/auth/reset-password";
}
