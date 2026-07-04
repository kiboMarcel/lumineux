namespace Lumineux.Application.Contracts.BureauProfiles;

/// <summary>Requête de création ou de mise à jour d'un profil du bureau (FR-001/FR-002/FR-015).</summary>
public sealed record BureauProfileWriteRequest(string Name, string? Description, IReadOnlyList<string> Permissions);

/// <summary>Vue résumée d'un profil (liste).</summary>
public sealed record BureauProfileSummaryResponse(
    int Id,
    string Name,
    string? Description,
    IReadOnlyList<string> Permissions,
    int MemberCount);

/// <summary>Vue publique d'un membre (FR-016 : aucune donnée sensible).</summary>
public sealed record MemberRefResponse(int Id, string Reference, string FullName, string Status);

/// <summary>Vue détaillée d'un profil (avec titulaires).</summary>
public sealed record BureauProfileDetailResponse(
    int Id,
    string Name,
    string? Description,
    IReadOnlyList<string> Permissions,
    int MemberCount,
    IReadOnlyList<MemberRefResponse> Members);

/// <summary>Requête d'attribution d'un profil à un membre.</summary>
public sealed record AssignProfileRequest(int ProfileId);

/// <summary>Réponse de consultation des profils d'un membre (avec droits effectifs).</summary>
public sealed record MemberProfilesResponse(
    MemberRefResponse Member,
    IReadOnlyList<BureauProfileSummaryResponse> Profiles,
    IReadOnlyList<string> EffectivePermissions);
