namespace Lumineux.Application.Abstractions;

/// <summary>Paramètres d'authentification (section "Auth").</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public int AccessTokenMinutes { get; set; } = 60;

    public int MaxFailedAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    public int PasswordMinLength { get; set; } = 8;

    /// <summary>Amorçage minimal des droits d'un compte bureau initial (feature 003, F1).</summary>
    public BootstrapOptions Bootstrap { get; set; } = new();
}

public sealed class BootstrapOptions
{
    public string? MemberReference { get; set; }

    public string[] Permissions { get; set; } = Array.Empty<string>();
}
