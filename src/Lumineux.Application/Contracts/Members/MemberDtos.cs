namespace Lumineux.Application.Contracts.Members;

/// <summary>Requête de création d'un membre (FR-001). `ConfirmDuplicate` gère l'homonymie (FR-007).</summary>
public sealed record CreateMemberRequest(
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
    bool ConfirmDuplicate = false);

/// <summary>Vue d'un membre exposée (aucun secret / hash).</summary>
public sealed record MemberResponse(
    int Id,
    string Reference,
    DateTime EntryDate,
    string LastName,
    string FirstName,
    string Gender,
    string? Mobile,
    string? Email,
    int? AntennaId,
    int? CivilityId,
    DateTime? BirthDate,
    int? BirthPlaceId,
    int? BirthCityId,
    string? Address,
    int? DistrictId,
    int? NationalityId,
    int? IntroducerId,
    string Status,
    string AccountActivationState);

/// <summary>Canal de transmission des identifiants initiaux (FR-011).</summary>
public static class CredentialsDelivery
{
    public const string EmailSent = "EmailSent";
    public const string BureauHandout = "BureauHandout";
}

/// <summary>
/// Réponse de création. `TemporaryPassword` n'est renseigné QUE si `CredentialsDelivery = BureauHandout`
/// (repli), une seule fois, pour remise par le bureau.
/// </summary>
public sealed record MemberCreatedResponse(
    MemberResponse Member,
    string LoginId,
    string CredentialsDelivery,
    string? TemporaryPassword);
