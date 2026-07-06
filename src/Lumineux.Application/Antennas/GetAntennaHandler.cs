using Lumineux.Application.Contracts.Antennas;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Antennas;

/// <summary>Cas d'usage : consultation d'une antenne par identifiant (feature 016). Accès gardé par
/// la policy <c>manage_referentials</c> sur le contrôleur.</summary>
public sealed class GetAntennaHandler
{
    private readonly IAntennaRepository _antennas;

    public GetAntennaHandler(IAntennaRepository antennas) => _antennas = antennas;

    public async Task<AntennaResponse> HandleAsync(int id, CancellationToken ct = default)
    {
        var antenna = await _antennas.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Antenne introuvable.");
        return antenna.ToResponse();
    }
}
