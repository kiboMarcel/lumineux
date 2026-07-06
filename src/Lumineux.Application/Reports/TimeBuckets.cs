using System.Globalization;
using Lumineux.Application.Contracts.Reports;

namespace Lumineux.Application.Reports;

/// <summary>
/// Génération et affectation d'intervalles temporels (feature 020) — logique **pure** et testable.
/// Mois calendaire (libellé <c>AAAA-MM</c>) et semaine <b>ISO 8601</b> (lundi→dimanche, libellé
/// <c>AAAA-Sww</c>). La bucketisation se fait en mémoire (portable SQLite/SQL Server).
/// </summary>
public static class TimeBuckets
{
    /// <summary>Un intervalle : sa date de début et son libellé.</summary>
    public sealed record Bucket(DateTime PeriodStart, string Label);

    /// <summary>Début de l'intervalle contenant <paramref name="date"/> (1er du mois / lundi ISO).</summary>
    public static DateTime BucketStart(DateTime date, TimeSeriesGranularity granularity) =>
        granularity == TimeSeriesGranularity.Month
            ? new DateTime(date.Year, date.Month, 1, 0, 0, 0, date.Kind)
            : MondayOf(date);

    /// <summary>Libellé de l'intervalle contenant <paramref name="date"/>.</summary>
    public static string Label(DateTime date, TimeSeriesGranularity granularity) =>
        granularity == TimeSeriesGranularity.Month
            ? date.ToString("yyyy-MM", CultureInfo.InvariantCulture)
            : $"{ISOWeek.GetYear(date):D4}-S{ISOWeek.GetWeekOfYear(date):D2}";

    /// <summary>Suite ordonnée et continue des intervalles couvrant [from, to] (bornes incluses).</summary>
    public static IReadOnlyList<Bucket> Generate(DateTime from, DateTime to, TimeSeriesGranularity granularity)
    {
        var buckets = new List<Bucket>();
        var cur = BucketStart(from, granularity);
        var end = BucketStart(to, granularity);

        while (cur <= end)
        {
            buckets.Add(new Bucket(cur, Label(cur, granularity)));
            cur = granularity == TimeSeriesGranularity.Month ? cur.AddMonths(1) : cur.AddDays(7);
        }

        return buckets;
    }

    /// <summary>Lundi (ISO 8601) de la semaine contenant <paramref name="date"/>.</summary>
    private static DateTime MondayOf(DateTime date)
    {
        // DayOfWeek : dimanche=0 … samedi=6 ; jours depuis lundi = (jour + 6) % 7.
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-daysSinceMonday);
    }
}
