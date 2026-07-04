using Lumineux.Domain.Abstractions;

namespace Lumineux.Domain.Entities;

/// <summary>
/// Profil du bureau (feature 004) : groupe nommé de droits fonctionnels attribuable à des membres.
/// Nom unique insensible à la casse ; droits validés contre un catalogue figé côté serveur.
/// </summary>
public class BureauProfile : AbstractEntity
{
    public const int NameMaxLength = 80;
    public const int DescriptionMaxLength = 255;

    public string Name { get; private set; } = default!;

    /// <summary>Nom en minuscules (invariant) pour l'unicité insensible à la casse.</summary>
    public string NameNormalized { get; private set; } = default!;

    public string? Description { get; private set; }

    public List<BureauProfilePermission> Permissions { get; private set; } = new();

    // Requis par EF Core.
    private BureauProfile() { }

    public static BureauProfile Create(string name, string? description, IEnumerable<string> permissions, IPermissionCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var profile = new BureauProfile();
        profile.Rename(name);
        profile.UpdateDescription(description);
        profile.SetPermissions(permissions, catalog);
        return profile;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Le nom du profil est requis.");
        }
        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainException($"Le nom du profil ne doit pas dépasser {NameMaxLength} caractères.");
        }
        Name = trimmed;
        NameNormalized = trimmed.ToLowerInvariant();
    }

    public void UpdateDescription(string? description)
    {
        if (description is not null && description.Length > DescriptionMaxLength)
        {
            throw new DomainException($"La description ne doit pas dépasser {DescriptionMaxLength} caractères.");
        }
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void SetPermissions(IEnumerable<string> permissions, IPermissionCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(permissions);

        var distinct = permissions
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var permission in distinct)
        {
            if (!catalog.Contains(permission))
            {
                throw new DomainException($"Droit inconnu : « {permission} ».");
            }
        }

        Permissions.Clear();
        foreach (var permission in distinct)
        {
            Permissions.Add(new BureauProfilePermission { Permission = permission });
        }
    }
}
