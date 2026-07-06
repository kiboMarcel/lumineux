using FluentAssertions;
using Lumineux.Application.Contracts.Reports;
using Lumineux.Application.Reports;
using Xunit;

namespace Lumineux.Application.Tests;

/// <summary>Génération/affectation d'intervalles temporels (feature 020) — logique pure.</summary>
public sealed class TimeBucketsTests
{
    [Fact]
    public void Month_generation_is_continuous_with_labels()
    {
        var buckets = TimeBuckets.Generate(new DateTime(2026, 1, 15), new DateTime(2026, 3, 10), TimeSeriesGranularity.Month);

        buckets.Select(b => b.Label).Should().Equal("2026-01", "2026-02", "2026-03");
        buckets[0].PeriodStart.Should().Be(new DateTime(2026, 1, 1));
        buckets[2].PeriodStart.Should().Be(new DateTime(2026, 3, 1));
    }

    [Fact]
    public void Month_bucket_start_is_first_of_month()
    {
        TimeBuckets.BucketStart(new DateTime(2026, 6, 23), TimeSeriesGranularity.Month)
            .Should().Be(new DateTime(2026, 6, 1));
    }

    [Fact]
    public void Week_bucket_start_is_the_monday_of_the_iso_week()
    {
        // 2026-06-24 est un mercredi → lundi ISO = 2026-06-22.
        var monday = TimeBuckets.BucketStart(new DateTime(2026, 6, 24), TimeSeriesGranularity.Week);
        monday.DayOfWeek.Should().Be(DayOfWeek.Monday);
        monday.Should().Be(new DateTime(2026, 6, 22));
    }

    [Fact]
    public void Week_generation_is_continuous_seven_days_apart()
    {
        var buckets = TimeBuckets.Generate(new DateTime(2026, 6, 1), new DateTime(2026, 6, 21), TimeSeriesGranularity.Week);

        buckets.Should().HaveCountGreaterThan(1);
        buckets.All(b => b.PeriodStart.DayOfWeek == DayOfWeek.Monday).Should().BeTrue();
        for (var i = 1; i < buckets.Count; i++)
        {
            (buckets[i].PeriodStart - buckets[i - 1].PeriodStart).Days.Should().Be(7);
        }
        buckets[0].Label.Should().MatchRegex(@"^\d{4}-S\d{2}$");
    }
}
