namespace Lumineux.Application.Contracts.Attendances;

/// <summary>Requête de scan du code QR par un membre (FR-008).</summary>
public sealed record ScanRequest(string Token);

/// <summary>Vue d'une présence exposée aux clients.</summary>
public sealed record AttendanceResponse(
    int Id,
    int SessionId,
    int MemberId,
    string? MemberFullName,
    DateTime ArrivalTime,
    DateTime? EndTime,
    string Source,
    string Status,
    int? OriginAntennaId);

/// <summary>Résultat d'un scan en ligne (créé vs déjà présent, pour choisir 201/200).</summary>
public sealed record ScanResult(AttendanceResponse Attendance, bool AlreadyPresent);

/// <summary>Un scan hors ligne mis en file côté client (FR-023).</summary>
public sealed record OfflineScanItem(string ClientOperationId, string Token, DateTime ClientArrivalTime);

/// <summary>Lot de scans hors ligne à synchroniser.</summary>
public sealed record OfflineScanBatchRequest(IReadOnlyList<OfflineScanItem> Items);

/// <summary>Résultat de synchronisation d'un élément du lot.</summary>
public sealed record OfflineScanResult(string ClientOperationId, string Outcome, string? Reason, int? AttendanceId);

/// <summary>Réponse de synchronisation d'un lot hors ligne.</summary>
public sealed record OfflineScanBatchResponse(IReadOnlyList<OfflineScanResult> Results);

/// <summary>Issues possibles d'un élément de synchronisation.</summary>
public static class OfflineScanOutcome
{
    public const string Created = "Created";
    public const string AlreadyPresent = "AlreadyPresent";
    public const string Rejected = "Rejected";
}
