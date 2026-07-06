using System.Globalization;
using System.Text;

namespace Lumineux.Application.Reports;

/// <summary>
/// Cas d'usage : export CSV de la synthèse par antenne (feature 018, US3). Réutilise la synthèse (US1)
/// pour garantir des chiffres identiques au JSON. Format tableur francophone : séparateur `;`, décimales
/// à la virgule. La BOM UTF-8 est ajoutée par le contrôleur.
/// </summary>
public sealed class ExportAntennaAttendanceCsvHandler
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    private readonly GetAntennaAttendanceSummaryHandler _summary;

    public ExportAntennaAttendanceCsvHandler(GetAntennaAttendanceSummaryHandler summary) => _summary = summary;

    public async Task<string> HandleAsync(DateTime from, DateTime to, int? antennaId, CancellationToken ct = default)
    {
        var summary = await _summary.HandleAsync(from, to, antennaId, ct);

        var sb = new StringBuilder();
        sb.Append("Antenne;Sessions;Présences valides;Moyenne par séance").Append('\n');
        foreach (var item in summary.Items)
        {
            sb.Append(Escape(item.AntennaLabel)).Append(';')
              .Append(item.SessionCount).Append(';')
              .Append(item.ValidAttendanceCount).Append(';')
              .Append(item.AverageValidPerSession.ToString("0.##", Fr))
              .Append('\n');
        }

        return sb.ToString();
    }

    /// <summary>Échappement CSV : entoure de guillemets si le champ contient `;`, `"` ou un saut de ligne.</summary>
    private static string Escape(string field)
    {
        if (field.Contains(';') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return '"' + field.Replace("\"", "\"\"") + '"';
        }

        return field;
    }
}
