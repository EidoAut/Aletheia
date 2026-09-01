using Aletheia.Analytics;
using Aletheia.Core;
using Aletheia.TimeSeries;

namespace Aletheia.Analytics.Tests;

public sealed class TimeDomainFeatureCalculatorTests
{
    private readonly TimeDomainFeatureCalculator calculator = new();

    [Fact]
    public void CalculateAutocorrelation_WithPerfectLag_ReturnsOne()
    {
        var series = new TimeSeries<double>(
        [
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 1), 1d),
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 2), 2d),
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 3), 3d),
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 4), 4d),
        ],
        ObservationFrequency.BusinessDaily);

        var result = this.calculator.CalculateAutocorrelation(series, 1);

        Assert.Equal(1d, result, 9);
    }

    [Fact]
    public void CalculateMovingAverage_WithMonthlySeries_PreservesMonthlyFrequency()
    {
        var series = new TimeSeries<double>(
        [
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 31), 1d),
            new TimeSeriesPoint<double>(new DateOnly(2024, 2, 29), 2d),
            new TimeSeriesPoint<double>(new DateOnly(2024, 3, 31), 3d),
        ],
        ObservationFrequency.Monthly);

        var result = this.calculator.CalculateMovingAverage(series, 2);

        Assert.Equal(ObservationFrequency.Monthly, result.ObservationFrequency);
    }

    [Fact]
    public void CalculateFirstOrderTrend_WithGrowingSeries_ReturnsPositiveSlope()
    {
        var series = new NavSeries(
        [
            new NavPoint(new DateOnly(2024, 1, 1), 100m),
            new NavPoint(new DateOnly(2024, 1, 2), 101m),
            new NavPoint(new DateOnly(2024, 1, 3), 102m),
        ],
        ObservationFrequency.BusinessDaily);

        var result = this.calculator.CalculateFirstOrderTrend(series, 3);

        Assert.True(result > 0d);
    }
}
