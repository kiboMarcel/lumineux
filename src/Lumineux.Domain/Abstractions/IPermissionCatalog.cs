namespace Lumineux.Domain.Abstractions;

/// <summary>
/// Référentiel figé des droits fonctionnels connus du système (feature 004, FR-008).
/// Les profils du bureau ne peuvent référencer que des droits présents dans ce catalogue.
/// </summary>
public interface IPermissionCatalog
{
    bool Contains(string permission);

    IReadOnlyList<PermissionDescriptor> All();
}

/// <summary>Description d'un droit fonctionnel : code technique + libellé humain.</summary>
public sealed record PermissionDescriptor(string Code, string Label);
