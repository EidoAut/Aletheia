using Aletheia.TimeSeries;

namespace Aletheia.TimeSeries.Tests;

public sealed class TimeSeriesTests
{
    [Fact]
    public void Slice_WithDateRange_ReturnsExpectedPoints()
    {
        var series = CreateSeries();

        var slice = series.Slice(new DateOnly(2024, 1, 2), new DateOnly(2024, 1, 3));

        Assert.Equal(2, slice.Count);
        Assert.Equal(2d, slice[0].Value);
        Assert.Equal(3d, slice[1].Value);
    }

    [Fact]
    public void RollingWindows_WithWindowSize_ReturnsChronologicalWindows()
    {
        var series = CreateSeries();

        var windows = series.RollingWindows(3);

        Assert.Equal(2, windows.Count);
        Assert.Equal(new DateOnly(2024, 1, 3), windows[0].EndDate);
        Assert.Equal(new DateOnly(2024, 1, 4), windows[1].EndDate);
    }

    private static TimeSeries<double> CreateSeries()
    {
        return new TimeSeries<double>(
        [
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 1), 1d),
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 2), 2d),
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 3), 3d),
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 4), 4d),
        ]);
    }
}
