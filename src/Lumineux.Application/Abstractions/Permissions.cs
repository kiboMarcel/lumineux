namespace Lumineux.Application.Abstractions;

/// <summary>Droits applicatifs (portés par les membres du bureau).</summary>
public static class Permissions
{
    /// <summary>Droit de gérer les présences (démarrer/clôturer une session, ajouter/retirer une présence).</summary>
    public const string ManageAttendance = "manage_attendance";

    /// <summary>Droit de gérer les membres (créer, corriger, consulter les fiches).</summary>
    public const string ManageMembers = "manage_members";
}
