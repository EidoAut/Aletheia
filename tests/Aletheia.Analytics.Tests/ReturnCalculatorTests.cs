using Aletheia.Analytics;
using Aletheia.Core;

namespace Aletheia.Analytics.Tests;

public sealed class ReturnCalculatorTests
{
    private readonly ReturnCalculator calculator = new();

    [Fact]
    public void CalculateSimpleReturns_WithKnownPrices_ReturnsExpectedReturn()
    {
        var series = CreateNavSeries(ObservationFrequency.BusinessDaily, 100m, 110m);

        var returns = this.calculator.CalculateSimpleReturns(series);

        Assert.Single(returns.Points);
        Assert.Equal(0.10d, returns[0].Value, 9);
        Assert.Equal(ObservationFrequency.BusinessDaily, returns.ObservationFrequency);
    }

    [Fact]
    public void CalculateLogReturns_WithKnownPrices_ReturnsExpectedLogReturn()
    {
        var series = CreateNavSeries(ObservationFrequency.Weekly, 100m, 110m);

        var returns = this.calculator.CalculateLogReturns(series);

        Assert.Equal(Math.Log(1.10d), returns[0].Value, 9);
        Assert.Equal(ObservationFrequency.Weekly, returns.ObservationFrequency);
    }

    [Fact]
    public void CalculateCumulativeReturn_WithKnownPrices_ReturnsExpectedValue()
    {
        var series = CreateNavSeries(ObservationFrequency.Monthly, 100m, 120m);

        var result = this.calculator.CalculateCumulativeReturn(series);

        Assert.Equal(0.20d, result, 9);
    }

    [Fact]
    public void CalculateRollingReturns_WithIrregularSource_PreservesIrregularFrequency()
    {
        var series = CreateNavSeries(ObservationFrequency.Irregular, 100m, 102m, 101m, 105m);

        var returns = this.calculator.CalculateRollingReturns(series, 2);

        Assert.Equal(ObservationFrequency.Irregular, returns.ObservationFrequency);
    }

    private static NavSeries CreateNavSeries(ObservationFrequency frequency, params decimal[] values)
    {
        return new NavSeries(
            values.Select((value, index) => new NavPoint(new DateOnly(2024, 1, 1).AddDays(index), value)),
            frequency);
    }
}
