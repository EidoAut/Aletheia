using Aletheia.Analytics;

namespace Aletheia.Analytics.Tests;

public sealed class ElapsedTimeAnnualizationEstimatorTests
{
    [Fact]
    public void EstimatePeriodsPerYear_UsesObservedIntervalsOverElapsedCalendarTime()
    {
        var dates = new[]
        {
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 11),
            new DateOnly(2024, 1, 31),
        };

        var result = new ElapsedTimeAnnualizationEstimator().EstimatePeriodsPerYear(dates);

        Assert.Equal(2d * 365.25d / 30d, result, 12);
    }

    [Fact]
    public void EstimatePeriodsPerYear_WithDuplicateAndUnorderedDates_UsesDistinctChronology()
    {
        var dates = new[]
        {
            new DateOnly(2024, 1, 31),
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 11),
            new DateOnly(2024, 1, 11),
        };

        var result = new ElapsedTimeAnnualizationEstimator().EstimatePeriodsPerYear(dates);

        Assert.Equal(2d * 365.25d / 30d, result, 12);
    }
}
