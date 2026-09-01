using Aletheia.Core;
using Aletheia.Validation;

namespace Aletheia.Validation.Tests;

public sealed class ScientificProtocolTests
{
    [Fact]
    public void FinalHoldoutSplitter_SeparatesDevelopmentFromFrozenHoldout()
    {
        var series = CreateSeries(120);

        var split = new FinalHoldoutSplitter().Split(
            series,
            new FinalHoldoutOptions
            {
                HoldoutObservationCount = 20,
                MinimumDevelopmentObservations = 80,
            });

        Assert.Equal(0, split.DevelopmentStartIndex);
        Assert.Equal(99, split.DevelopmentEndIndex);
        Assert.Equal(100, split.HoldoutStartIndex);
        Assert.Equal(119, split.HoldoutEndIndex);
        Assert.Equal(100, split.DevelopmentSeries.Count);
        Assert.Equal(20, split.HoldoutSeries.Count);
        Assert.True(split.DevelopmentSeries.EndDate < split.HoldoutSeries.StartDate);
    }

    private static NavSeries CreateSeries(int count)
    {
        var start = new DateOnly(2024, 1, 1);
        return new NavSeries(
            Enumerable.Range(0, count).Select(index => new NavPoint(start.AddDays(index), 100m + index)),
            ObservationFrequency.Daily);
    }
}
