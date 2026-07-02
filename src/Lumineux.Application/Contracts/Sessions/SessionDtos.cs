namespace Lumineux.Application.Contracts.Sessions;

/// <summary>Requête de démarrage d'une session (FR-001).</summary>
public sealed record StartSessionRequest(int AntennaId, DateTime MeetingDate, int? QrStepSeconds);

/// <summary>Vue d'une session exposée aux clients — le secret QR n'y figure jamais.</summary>
public sealed record SessionResponse(
    int Id,
    int AntennaId,
    DateTime MeetingDate,
    DateTime StartTime,
    DateTime? EndTime,
    string Status,
    int OpenedByMemberId,
    int? ClosedByMemberId,
    int AttendanceCount);

/// <summary>Jeton QR courant à afficher par le bureau (FR-013).</summary>
public sealed record QrTokenResponse(string Token, int StepSeconds, DateTime ExpiresAt);
