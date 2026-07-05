namespace Lumineux.Application.Contracts.Setup;

/// <summary>Requête d'installation du premier administrateur (feature 005, FR-002).</summary>
public sealed record InstallFirstAdminRequest(
    string LastName,
    string FirstName,
    string Gender,
    string Password,
    string? Email = null,
    string? Mobile = null);

/// <summary>
/// Statut d'installation de l'instance (feature 012). Indicateur booléen uniquement — aucune donnée
/// sensible. <c>Installed = true</c> ssi au moins un administrateur actif existe.
/// </summary>
public sealed record SetupStatusResponse(bool Installed);
