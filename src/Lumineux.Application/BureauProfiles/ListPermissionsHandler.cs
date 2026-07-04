using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.BureauProfiles;

/// <summary>Référentiel figé des droits fonctionnels (US4, FR-008).</summary>
public sealed class ListPermissionsHandler
{
    private readonly IPermissionCatalog _catalog;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;

    public ListPermissionsHandler(IPermissionCatalog catalog, ICurrentUser user, IAuditLogger audit)
    {
        _catalog = catalog;
        _user = user;
        _audit = audit;
    }

    public IReadOnlyList<PermissionDescriptor> Handle()
    {
        if (!ReadAccess.HasReadAccess(_user))
        {
            _audit.Refused("ListPermissions", "Droit manque");
            throw new ForbiddenException("Droit requis pour consulter le référentiel des droits.");
        }
        return _catalog.All();
    }
}
