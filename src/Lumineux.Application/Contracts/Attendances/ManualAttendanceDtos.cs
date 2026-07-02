namespace Lumineux.Application.Contracts.Attendances;

/// <summary>Requête d'ajout manuel d'une présence par le bureau (FR-014).</summary>
public sealed record ManualAttendanceRequest(int MemberId, DateTime? ArrivalTime);

/// <summary>Résultat d'un ajout manuel (créé vs déjà présent, pour choisir 201/200).</summary>
public sealed record ManualAttendanceResult(AttendanceResponse Attendance, bool AlreadyPresent);

/// <summary>Liste des présences d'une session + décompte des présences valides (FR-021/022).</summary>
public sealed record AttendanceListResponse(int SessionId, int ValidCount, IReadOnlyList<AttendanceResponse> Items);

/// <summary>Filtre de statut pour la consultation des présences.</summary>
public static class AttendanceStatusFilter
{
    public const string Valid = "Valid";
    public const string Cancelled = "Cancelled";
    public const string All = "All";
}
