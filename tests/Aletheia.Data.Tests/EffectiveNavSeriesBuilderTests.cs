using Aletheia.Core;
using Aletheia.Data;

namespace Aletheia.Data.Tests;

public sealed class EffectiveNavSeriesBuilderTests
{
    [Fact]
    public void Build_RemovesWeekendCarryForwardRowsWithoutChangingSourceSeries()
    {
        var start = new DateOnly(2024, 1, 1);
        var value = 100m;
        var points = Enumerable.Range(0, 14)
            .Select(offset =>
            {
                var date = start.AddDays(offset);
                if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                {
                    value += 1m;
                }

                return new NavPoint(date, value);
            })
            .ToArray();
        var source = new NavSeries(points, ObservationFrequencyDetector.Detect(points));

        var result = new EffectiveNavSeriesBuilder().Build(source);

        Assert.Equal(ObservationFrequency.Daily, source.ObservationFrequency);
        Assert.Equal(14, result.SourceObservationCount);
        Assert.Equal(10, result.EffectiveObservationCount);
        Assert.Equal(4, result.SyntheticObservationCount);
        Assert.Equal(ObservationFrequency.BusinessDaily, result.NavSeries.ObservationFrequency);
        Assert.DoesNotContain(result.NavSeries.Points, point =>
            point.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
        Assert.Contains("carry-forward", result.Policy, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_KeepsWeekendRowsWhenValueChanges()
    {
        var points = Enumerable.Range(0, 7)
            .Select(offset => new NavPoint(new DateOnly(2024, 1, 1).AddDays(offset), 100m + offset))
            .ToArray();
        var source = new NavSeries(points, ObservationFrequencyDetector.Detect(points));

        var result = new EffectiveNavSeriesBuilder().Build(source);

        Assert.Equal(source.Count, result.EffectiveObservationCount);
        Assert.Equal(0, result.SyntheticObservationCount);
        Assert.Equal(ObservationFrequency.Daily, result.NavSeries.ObservationFrequency);
    }
}
