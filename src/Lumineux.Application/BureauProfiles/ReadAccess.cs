using Lumineux.Application.Abstractions;

namespace Lumineux.Application.BureauProfiles;

/// <summary>
/// Autorisation de lecture partagée pour US4 : <c>manage_bureau_profiles</c> OU <c>manage_members</c>
/// (FR-009). Les écritures restent strictement réservées à <c>manage_bureau_profiles</c>.
/// </summary>
internal static class ReadAccess
{
    public static bool HasReadAccess(ICurrentUser user) =>
        user.HasPermission(Permissions.ManageBureauProfiles)
        || user.HasPermission(Permissions.ManageMembers);
}
