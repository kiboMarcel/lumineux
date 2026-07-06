namespace Lumineux.Application.Contracts.Reports;

/// <summary>Ligne de synthèse d'affluence pour une antenne, sur la période demandée (feature 018, US1).</summary>
public sealed record AntennaAttendanceSummaryItem(
    int AntennaId,
    string AntennaLabel,
    int SessionCount,
    int ValidAttendanceCount,
    decimal AverageValidPerSession);

/// <summary>Synthèse d'affluence par antenne sur une plage de dates.</summary>
public sealed record AntennaAttendanceSummaryResponse(
    DateTime From,
    DateTime To,
    IReadOnlyList<AntennaAttendanceSummaryItem> Items);

/// <summary>
/// Taux d'assiduité d'un membre sur une période (US2). Le dénominateur est le nombre de sessions de
/// l'antenne d'origine du membre sur la période ; <c>Rate</c> est une fraction 0..1 (0 si aucune
/// session éligible).
/// </summary>
public sealed record MemberAttendanceRateResponse(
    int MemberId,
    string MemberFullName,
    DateTime From,
    DateTime To,
    int ValidAttendanceCount,
    int EligibleSessionCount,
    decimal Rate);
