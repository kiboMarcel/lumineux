namespace Lumineux.Application.Contracts.Members;

/// <summary>Requête de correction d'un membre (FR-014). La référence n'est pas modifiable.</summary>
public sealed record UpdateMemberRequest(
    string LastName,
    string FirstName,
    string Gender,
    string? Mobile,
    string? Email,
    int AntennaId,
    int? CivilityId,
    DateTime? BirthDate,
    int? BirthPlaceId,
    int? BirthCityId,
    string? Address,
    int? DistrictId,
    int? NationalityId,
    int? IntroducerId,
    string? Profession = null);

/// <summary>Élément de liste de membres (recherche).</summary>
public sealed record MemberListItem(
    int Id,
    string Reference,
    string LastName,
    string FirstName,
    string? Mobile,
    string? Email,
    int? AntennaId,
    string Status);

/// <summary>Résultat paginé de recherche de membres (FR-013).</summary>
public sealed record MemberListResponse(int Page, int PageSize, int Total, IReadOnlyList<MemberListItem> Items);

/// <summary>
/// Entrée de recherche membre allégée (feature 015). Champs minimaux d'identification — aucune
/// coordonnée ni donnée personnelle superflue.
/// </summary>
public sealed record MemberLookupResponse(int Id, string Reference, string FullName, string Status);
