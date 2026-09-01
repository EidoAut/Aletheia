using Aletheia.Analytics;
using Aletheia.TimeSeries;

namespace Aletheia.Analytics.Tests;

public sealed class NumericalDerivativeCalculatorTests
{
    [Fact]
    public void CalculateSecondDerivative_WithQuadraticSignal_ReturnsPositiveValue()
    {
        var calculator = new NumericalDerivativeCalculator();
        var series = new TimeSeries<double>(
        [
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 1), 0d),
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 2), 1d),
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 3), 4d),
            new TimeSeriesPoint<double>(new DateOnly(2024, 1, 4), 9d),
        ]);

        var result = calculator.CalculateSecondDerivative(series, 1);

        Assert.All(result.Points, point => Assert.True(point.Value > 0d));
    }
}
