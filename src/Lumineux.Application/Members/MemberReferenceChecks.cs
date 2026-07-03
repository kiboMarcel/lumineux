using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Members;

/// <summary>Vérifie l'existence des références rattachées à un membre (FR-005), partagé création/correction.</summary>
internal static class MemberReferenceChecks
{
    public static async Task EnsureExistAsync(
        IReferenceLookupRepository lookup,
        int antennaId,
        int? civilityId,
        int? nationalityId,
        int? birthPlaceId,
        int? birthCityId,
        int? districtId,
        int? introducerId,
        CancellationToken ct)
    {
        if (!await lookup.AntennaExistsAsync(antennaId, ct))
        {
            throw new NotFoundException("Antenne introuvable.");
        }

        if (civilityId is { } civ && !await lookup.CivilityExistsAsync(civ, ct))
        {
            throw new NotFoundException("Civilité introuvable.");
        }

        if (nationalityId is { } nat && !await lookup.CountryExistsAsync(nat, ct))
        {
            throw new NotFoundException("Nationalité (pays) introuvable.");
        }

        if (birthPlaceId is { } bp && !await lookup.CityExistsAsync(bp, ct))
        {
            throw new NotFoundException("Lieu de naissance introuvable.");
        }

        if (birthCityId is { } bc && !await lookup.CityExistsAsync(bc, ct))
        {
            throw new NotFoundException("Ville de naissance introuvable.");
        }

        if (districtId is { } dist && !await lookup.DistrictExistsAsync(dist, ct))
        {
            throw new NotFoundException("Quartier (district) introuvable.");
        }

        if (introducerId is { } intro && !await lookup.MemberExistsAsync(intro, ct))
        {
            throw new NotFoundException("Membre introducteur introuvable.");
        }
    }
}
