namespace Lumineux.Domain.Abstractions;

/// <summary>Ligne agrégée d'affluence par antenne (feature 018) — présences valides uniquement.</summary>
public sealed record AntennaSummaryRow(int AntennaId, string AntennaLabel, int SessionCount, int ValidAttendanceCount);

/// <summary>
/// Éléments du taux d'un membre : nom, antenne d'origine (dénominateur), présences valides et sessions
/// éligibles (sessions de l'antenne d'origine sur la période).
/// </summary>
public sealed record MemberRateData(string MemberFullName, int? OriginAntennaId, int ValidAttendanceCount, int EligibleSessionCount);

/// <summary>
/// Port de lecture/agrégation pour les rapports de présence (feature 018). **Lecture seule** : aucune
/// écriture, aucune migration. Les décomptes ne considèrent que les présences valides.
/// </summary>
public interface IAttendanceReportRepository
{
    /// <summary>Synthèse par antenne sur [from, to] (bornes de réunion inclusives), filtrable par antenne.</summary>
    Task<IReadOnlyList<AntennaSummaryRow>> GetAntennaSummaryAsync(
        DateTime from, DateTime to, int? antennaId, CancellationToken ct = default);

    /// <summary>Éléments du taux d'un membre sur [from, to] ; <c>null</c> si le membre est introuvable.</summary>
    Task<MemberRateData?> GetMemberRateDataAsync(
        int memberId, DateTime from, DateTime to, CancellationToken ct = default);
}
