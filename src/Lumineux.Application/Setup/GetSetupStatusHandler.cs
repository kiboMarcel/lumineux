using Lumineux.Application.Contracts.Setup;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Setup;

/// <summary>
/// Cas d'usage : statut d'installation de l'instance (feature 012). Lecture seule et anonyme.
/// <c>Installed = true</c> si et seulement s'il existe au moins un administrateur actif — réutilise
/// le MÊME décompte que le verrou d'installation (<see cref="InstallFirstAdminHandler"/>) pour rester
/// cohérent par construction. N'expose qu'un booléen (aucune donnée sensible, aucune énumération).
/// </summary>
public sealed class GetSetupStatusHandler
{
    private readonly IBureauProfileRepository _profiles;

    public GetSetupStatusHandler(IBureauProfileRepository profiles) => _profiles = profiles;

    public async Task<SetupStatusResponse> HandleAsync(CancellationToken ct = default)
    {
        var activeAdmins = await _profiles.CountActiveAdministratorsAsync(ct: ct);
        return new SetupStatusResponse(activeAdmins > 0);
    }
}
