using Aletheia.Analytics;
using Aletheia.Core;
using Aletheia.TimeSeries;

namespace Aletheia.Analytics.Tests;

public sealed class RiskMetricsCalculatorTests
{
    private readonly RiskMetricsCalculator calculator = new();

    [Fact]
    public void CalculateMaximumDrawdown_WithKnownPath_ReturnsWorstPeakToTroughLoss()
    {
        var series = new NavSeries(
        [
            new NavPoint(new DateOnly(2024, 1, 1), 100m),
            new NavPoint(new DateOnly(2024, 1, 2), 120m),
            new NavPoint(new DateOnly(2024, 1, 3), 90m),
            new NavPoint(new DateOnly(2024, 1, 4), 130m),
        ],
        ObservationFrequency.BusinessDaily);

        var result = this.calculator.CalculateMaximumDrawdown(series);

        Assert.Equal(-0.25d, result.MaximumDrawdown, 9);
        Assert.Equal(new DateOnly(2024, 1, 2), result.PeakDate);
        Assert.Equal(new DateOnly(2024, 1, 3), result.TroughDate);
    }

    [Fact]
    public void CalculateSharpeRatio_WithZeroVolatility_ReturnsZero()
    {
        var returns = new TimeSeries<double>(
        [
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 1), 0.01d),
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 2), 0.01d),
        ],
        ObservationFrequency.BusinessDaily);

        var result = this.calculator.CalculateSharpeRatio(returns);

        Assert.Equal(0d, result);
    }

    [Fact]
    public void CalculateAnnualizedVolatility_UsesObservationFrequencyConvention()
    {
        var businessDaily = CreateReturns(ObservationFrequency.BusinessDaily);
        var monthly = CreateReturns(ObservationFrequency.Monthly);

        var businessDailyVolatility = this.calculator.CalculateAnnualizedVolatility(businessDaily);
        var monthlyVolatility = this.calculator.CalculateAnnualizedVolatility(monthly);

        Assert.True(businessDailyVolatility > monthlyVolatility);
        Assert.Equal(
            this.calculator.CalculateVolatility(businessDaily) * Math.Sqrt(252d),
            businessDailyVolatility,
            12);
        Assert.Equal(
            this.calculator.CalculateVolatility(monthly) * Math.Sqrt(12d),
            monthlyVolatility,
            12);
    }

    [Fact]
    public void CalculateAnnualizedVolatility_WithIrregularFrequency_RequiresExplicitConvention()
    {
        var returns = CreateReturns(ObservationFrequency.Irregular);

        Assert.Throws<InvalidOperationException>(() => this.calculator.CalculateAnnualizedVolatility(returns));
        Assert.True(this.calculator.CalculateAnnualizedVolatility(returns, periodsPerYear: 10d) > 0d);
    }

    [Fact]
    public void CalculateAnnualizedVolatility_WithIrregularEstimator_UsesElapsedDates()
    {
        var returns = new TimeSeries<double>(
        [
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 1), -0.01d),
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 11), 0.00d),
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 31), 0.01d),
        ],
        ObservationFrequency.Irregular);
        var calculator = new RiskMetricsCalculator(
            irregularAnnualizationEstimator: new ElapsedTimeAnnualizationEstimator());
        var expectedPeriodsPerYear = 2d * 365.25d / 30d;

        var result = calculator.CalculateAnnualizedVolatility(returns);

        Assert.Equal(calculator.CalculateVolatility(returns) * Math.Sqrt(expectedPeriodsPerYear), result, 12);
    }

    private static TimeSeries<double> CreateReturns(ObservationFrequency frequency)
    {
        return new TimeSeries<double>(
        [
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 1), -0.01d),
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 2), 0.00d),
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 3), 0.01d),
        ],
        frequency);
    }
}
