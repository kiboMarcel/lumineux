using Lumineux.Application.Contracts.Antennas;
using Lumineux.Domain.Entities;

namespace Lumineux.Application.Antennas;

/// <summary>Projection entité → DTO (aucune entité de persistance exposée, Principe V).</summary>
internal static class AntennaMapping
{
    public static AntennaResponse ToResponse(this Antenna a) =>
        new(a.Id, a.Code, a.Label, a.District, a.Status);
}
