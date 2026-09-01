using Aletheia.Analytics;
using Aletheia.Core;
using Aletheia.TimeSeries;

namespace Aletheia.Analytics.Tests;

public sealed class RollingAnalyticsCalculatorTests
{
    [Fact]
    public void RollingMetrics_AreDatedAtWindowEnd()
    {
        var calculator = new RollingAnalyticsCalculator();
        var returns = new TimeSeries<double>(
            Enumerable.Range(0, 5).Select(index =>
                new TimeSeriesPoint<double>(new DateOnly(2024, 1, 1).AddDays(index), 0.01d * index)),
            ObservationFrequency.Daily);

        var rolling = calculator.RollingSkewness(returns, 3);

        Assert.Equal(3, rolling.Count);
        Assert.Equal(new DateOnly(2024, 1, 3), rolling[0].Date);
        Assert.All(rolling.Points, point => Assert.True(double.IsFinite(point.Value)));
    }

    [Fact]
    public void RiskMetrics_ExposeTailAndDrawdownDiagnostics()
    {
        var risk = new RiskMetricsCalculator();
        var returns = new TimeSeries<double>(
            new[]
            {
                new TimeSeriesPoint<double>(new DateOnly(2024, 1, 1), -0.10d),
                new TimeSeriesPoint<double>(new DateOnly(2024, 1, 2), -0.03d),
                new TimeSeriesPoint<double>(new DateOnly(2024, 1, 3), 0.02d),
                new TimeSeriesPoint<double>(new DateOnly(2024, 1, 4), 0.04d),
            },
            ObservationFrequency.Daily);
        var nav = new NavSeries(
            new[]
            {
                new NavPoint(new DateOnly(2024, 1, 1), 100m),
                new NavPoint(new DateOnly(2024, 1, 2), 90m),
                new NavPoint(new DateOnly(2024, 1, 3), 95m),
                new NavPoint(new DateOnly(2024, 1, 4), 110m),
            },
            ObservationFrequency.Daily);

        Assert.True(risk.CalculateHistoricalValueAtRisk(returns, 0.95d) > 0d);
        Assert.True(risk.CalculateExpectedShortfall(returns, 0.95d) > 0d);
        Assert.True(risk.CalculateUlcerIndex(nav) > 0d);
        Assert.True(risk.CalculateDrawdownStatistics(nav).MaximumDurationDays > 0);
        Assert.True(double.IsFinite(risk.CalculateOmegaRatio(returns)));
    }
}
