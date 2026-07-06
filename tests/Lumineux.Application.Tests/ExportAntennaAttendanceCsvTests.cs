using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Reports;
using Lumineux.Domain.Abstractions;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

/// <summary>Export CSV de la synthèse par antenne (feature 018, US3) — cohérence avec la synthèse.</summary>
public sealed class ExportAntennaAttendanceCsvTests
{
    private static readonly DateTime From = new(2026, 6, 1);
    private static readonly DateTime To = new(2026, 6, 30);

    private readonly IAttendanceReportRepository _reports = Substitute.For<IAttendanceReportRepository>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();

    private ExportAntennaAttendanceCsvHandler Handler()
    {
        var summary = new GetAntennaAttendanceSummaryHandler(_reports, _user, new ReportPeriodValidator());
        return new ExportAntennaAttendanceCsvHandler(summary);
    }

    [Fact]
    public async Task Renders_header_and_one_line_per_antenna_with_consistent_values()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        _reports.GetAntennaSummaryAsync(From, To, null, Arg.Any<CancellationToken>())
            .Returns(new List<AntennaSummaryRow>
            {
                new(1, "Antenne 1", 2, 3),
                new(2, "Antenne; \"spéciale\"", 4, 4),
            });

        var csv = await Handler().HandleAsync(From, To, null);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines[0].Should().Be("Antenne;Sessions;Présences valides;Moyenne par séance");
        lines.Should().HaveCount(3); // en-tête + 2 antennes
        lines[1].Should().Be("Antenne 1;2;3;1,5"); // moyenne 3/2 avec virgule décimale (fr)
        // Champ contenant ; et " → entouré de guillemets, guillemets doublés.
        lines[2].Should().StartWith("\"Antenne; \"\"spéciale\"\"\";4;4;1");
    }
}
