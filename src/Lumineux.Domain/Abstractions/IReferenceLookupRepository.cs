namespace Lumineux.Domain.Abstractions;

/// <summary>
/// Vérifie l'existence des entités de référence rattachées à un membre (FR-005) :
/// antenne, civilité, pays (nationalité), ville, district, et membre introducteur.
/// </summary>
public interface IReferenceLookupRepository
{
    Task<bool> AntennaExistsAsync(int id, CancellationToken ct = default);

    Task<bool> CivilityExistsAsync(int id, CancellationToken ct = default);

    Task<bool> CountryExistsAsync(int id, CancellationToken ct = default);

    Task<bool> CityExistsAsync(int id, CancellationToken ct = default);

    Task<bool> DistrictExistsAsync(int id, CancellationToken ct = default);

    Task<bool> MemberExistsAsync(int id, CancellationToken ct = default);
}
