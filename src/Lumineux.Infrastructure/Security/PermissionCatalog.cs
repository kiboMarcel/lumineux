using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Infrastructure.Security;

/// <summary>
/// Référentiel figé des droits fonctionnels connus du système (feature 004, FR-008).
/// Toute évolution du catalogue impose une modification du code (et généralement de nouvelles
/// policies/endpoints qui s'appuient sur le droit ajouté).
/// </summary>
public sealed class PermissionCatalog : IPermissionCatalog
{
    private static readonly IReadOnlyList<PermissionDescriptor> Entries = new List<PermissionDescriptor>
    {
        new(Permissions.ManageAttendance, "Gérer les présences"),
        new(Permissions.ManageMembers, "Gérer les membres"),
        new(Permissions.ManageBureauProfiles, "Gérer les profils du bureau"),
    };

    private static readonly HashSet<string> Codes =
        new(Entries.Select(e => e.Code), StringComparer.Ordinal);

    public bool Contains(string permission) =>
        !string.IsNullOrWhiteSpace(permission) && Codes.Contains(permission);

    public IReadOnlyList<PermissionDescriptor> All() => Entries;
}
