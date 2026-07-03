namespace Lumineux.Infrastructure.Email;

/// <summary>Paramètres d'e-mail (section "Email"). Les secrets SMTP proviennent de la configuration.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>"Logging" (dev) ou "Smtp" (prod).</summary>
    public string Provider { get; set; } = "Logging";

    public string FromAddress { get; set; } = "no-reply@lumineux.example";

    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
