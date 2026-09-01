using Aletheia.Core;
using Aletheia.Forecasting;

namespace Aletheia.Forecasting.Tests;

public sealed class NaiveForecastBaselineTests
{
    [Fact]
    public void Forecast_WithGrowingSeries_ReturnsRequestedHorizons()
    {
        var series = new NavSeries(
            Enumerable.Range(0, 120).Select(index =>
                new NavPoint(new DateOnly(2024, 1, 1).AddDays(index), 100m + index)),
            ObservationFrequency.Daily);
        var model = new NaiveForecastBaseline();

        var result = model.Forecast(series, [ForecastHorizon.Observations(7), ForecastHorizon.CalendarDays(30)]);

        Assert.Equal(2, result.Distributions.Count);
        Assert.All(result.Distributions, distribution => Assert.True(distribution.Percentiles.Count > 0));
        Assert.Equal(7, result.Distributions[0].HorizonResolution.EffectiveObservationCount);
        Assert.Equal(30, result.Distributions[1].HorizonResolution.EffectiveObservationCount);
        Assert.Equal(ObservationFrequency.Daily, result.Distributions[1].HorizonResolution.ObservationFrequency);
    }
}
