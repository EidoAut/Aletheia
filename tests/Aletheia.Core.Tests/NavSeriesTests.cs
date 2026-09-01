using Aletheia.Core;

namespace Aletheia.Core.Tests;

public sealed class NavSeriesTests
{
    [Fact]
    public void Constructor_WithUnorderedPoints_SortsByDate()
    {
        var series = new NavSeries(
        [
            new NavPoint(new DateOnly(2024, 1, 3), 102m),
            new NavPoint(new DateOnly(2024, 1, 1), 100m),
            new NavPoint(new DateOnly(2024, 1, 2), 101m),
        ]);

        Assert.Equal(new DateOnly(2024, 1, 1), series[0].Date);
        Assert.Equal(new DateOnly(2024, 1, 3), series[2].Date);
    }

    [Fact]
    public void Constructor_WithDuplicateDates_Throws()
    {
        var points = new[]
        {
            new NavPoint(new DateOnly(2024, 1, 1), 100m),
            new NavPoint(new DateOnly(2024, 1, 1), 101m),
        };

        Assert.Throws<ArgumentException>(() => new NavSeries(points));
    }
}
