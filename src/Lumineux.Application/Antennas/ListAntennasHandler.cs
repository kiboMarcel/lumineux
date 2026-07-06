using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Antennas;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Antennas;

/// <summary>
/// Cas d'usage : liste de gestion des antennes (feature 016, US4, FR-008). Renvoie les antennes
/// <b>actives ET inactives</b> avec leur statut — distinct de la lecture publique des actives (010).
/// </summary>
public sealed class ListAntennasHandler
{
    private readonly IAntennaRepository _antennas;
    private readonly ICurrentUser _user;

    public ListAntennasHandler(IAntennaRepository antennas, ICurrentUser user)
    {
        _antennas = antennas;
        _user = user;
    }

    public async Task<IReadOnlyList<AntennaResponse>> HandleAsync(CancellationToken ct = default)
    {
        if (!_user.HasPermission(Permissions.ManageReferentials))
        {
            throw new ForbiddenException("Droit requis pour gérer les référentiels.");
        }

        var all = await _antennas.ListAllAsync(ct);
        return all.Select(a => a.ToResponse()).ToList();
    }
}
