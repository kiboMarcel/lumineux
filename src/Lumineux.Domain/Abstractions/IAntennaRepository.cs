using Lumineux.Domain.Entities;

namespace Lumineux.Domain.Abstractions;

/// <summary>
/// Port de persistance pour la <b>gestion</b> des antennes (feature 016) : lecture (y compris
/// inactives), unicité du code, écriture. Distinct de <see cref="IAntennaReadRepository"/> (existence)
/// et de la lecture publique des actives (feature 010).
/// </summary>
public interface IAntennaRepository
{
    Task<Antenna?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Antenne portant ce code (comparaison insensible aux espaces) — pour l'unicité.</summary>
    Task<Antenna?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Toutes les antennes (actives ET inactives), pour la gestion.</summary>
    Task<IReadOnlyList<Antenna>> ListAllAsync(CancellationToken ct = default);

    Task AddAsync(Antenna antenna, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
