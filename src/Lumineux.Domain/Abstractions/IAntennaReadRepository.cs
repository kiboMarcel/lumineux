namespace Lumineux.Domain.Abstractions;

/// <summary>Accès en lecture aux antennes (réutilisées, non gérées par cette fonctionnalité).</summary>
public interface IAntennaReadRepository
{
    Task<bool> ExistsAsync(int antennaId, CancellationToken ct = default);
}
