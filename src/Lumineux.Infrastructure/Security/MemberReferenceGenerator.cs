using System.Globalization;
using System.Text.RegularExpressions;
using Lumineux.Domain.Abstractions;
using Lumineux.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lumineux.Infrastructure.Security;

/// <summary>Options de génération de la référence membre (section "MemberReference").</summary>
public sealed class MemberReferenceOptions
{
    public const string SectionName = "MemberReference";

    public string Format { get; set; } = "LUM-{yyyy}-{seq:00000}";
}

/// <summary>
/// Génère une référence membre unique du type <c>LUM-2026-00042</c>. La séquence est dérivée du
/// nombre de membres de l'année ; l'index unique en base sert de filet de sécurité.
/// </summary>
public sealed partial class MemberReferenceGenerator : IMemberReferenceGenerator
{
    private readonly AppDbContext _db;
    private readonly string _format;

    public MemberReferenceGenerator(AppDbContext db, IOptions<MemberReferenceOptions> options)
    {
        _db = db;
        _format = options.Value.Format;
    }

    public async Task<string> NextAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        var year = nowUtc.Year;
        var countThisYear = await _db.Members.CountAsync(m => m.EntryDate.Year == year, ct);
        return Build(_format, year, countThisYear + 1);
    }

    private static string Build(string format, int year, int seq)
    {
        var result = format.Replace("{yyyy}", year.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        var match = SeqToken().Match(result);
        if (match.Success)
        {
            var width = match.Groups[1].Value.Length;
            return result.Replace(match.Value, seq.ToString("D" + width, CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        return result.Replace("{seq}", seq.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\{seq:(0+)\}")]
    private static partial Regex SeqToken();
}
