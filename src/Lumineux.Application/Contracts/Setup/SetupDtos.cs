namespace Lumineux.Application.Contracts.Setup;

/// <summary>Requête d'installation du premier administrateur (feature 005, FR-002).</summary>
public sealed record InstallFirstAdminRequest(
    string LastName,
    string FirstName,
    string Gender,
    string Password,
    string? Email = null,
    string? Mobile = null);
