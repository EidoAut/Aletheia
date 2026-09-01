using Aletheia.Core;
using Aletheia.Dynamics;

namespace Aletheia.Dynamics.Tests;

public sealed class AutoregressiveStateModelTests
{
    [Fact]
    public void ForecastExpectedLogReturns_UsesRecursiveArOneEquations()
    {
        var model = AutoregressiveStateModel.FromParameters(0.01d, 0.5d, 0.0001d);

        var expected = model.ForecastExpectedLogReturns(0.04d, 3);

        Assert.Equal(0.03d, expected[0], 9);
        Assert.Equal(0.025d, expected[1], 9);
        Assert.Equal(0.0225d, expected[2], 9);
    }

    [Theory]
    [InlineData(0d, true)]
    [InlineData(0.5d, true)]
    [InlineData(-0.5d, true)]
    [InlineData(0.99d, true)]
    [InlineData(1.0d, false)]
    public void FromParameters_ReportsStationarity(double phi, bool expectedStationary)
    {
        var model = AutoregressiveStateModel.FromParameters(0.01d, phi, 0.0001d);

        Assert.Equal(expectedStationary, model.IsStationary);
    }

    [Fact]
    public void Forecast_WithLogReturnState_ReturnsCumulativeSimpleReturn()
    {
        var model = AutoregressiveStateModel.FromParameters(0.01d, 0.5d, 0.0001d);
        var state = new DynamicState(
            new DateOnly(2024, 1, 1),
            new Dictionary<StateDimension, double> { [StandardStateDimensions.LogReturn] = 0.04d },
            1d);

        var forecast = model.Forecast(state, ForecastHorizon.Observations(3));

        Assert.Equal(0.0775d, forecast.CumulativeExpectedLogReturn, 9);
        Assert.Equal(Math.Exp(0.0775d) - 1d, forecast.MedianSimpleReturn, 9);
        Assert.Equal(forecast.MedianSimpleReturn, forecast.PointForecastSimpleReturn, 12);
        Assert.True(forecast.ExpectedSimpleReturn > forecast.MedianSimpleReturn);
        Assert.True(forecast.SimpleReturnQuantiles[90] > forecast.SimpleReturnQuantiles[10]);
        Assert.True(forecast.CumulativeLogReturnVariance > 0d);
    }

    [Fact]
    public void Forecast_WithCalendarHorizon_ThrowsBecauseArOperatesOnObservations()
    {
        var model = AutoregressiveStateModel.FromParameters(0.01d, 0.5d, 0.0001d);
        var state = new DynamicState(
            new DateOnly(2024, 1, 1),
            new Dictionary<StateDimension, double> { [StandardStateDimensions.LogReturn] = 0.04d },
            1d);

        Assert.Throws<ArgumentException>(() => model.Forecast(state, ForecastHorizon.CalendarDays(30)));
    }

    [Fact]
    public void Forecast_WithoutLogReturn_ThrowsCompatibilityException()
    {
        var model = AutoregressiveStateModel.FromParameters(0.01d, 0.5d, 0.0001d);
        var state = new DynamicState(
            new DateOnly(2024, 1, 1),
            new Dictionary<StateDimension, double> { [StandardStateDimensions.SimpleReturn] = 0.04d },
            1d);

        Assert.Throws<IncompatibleDynamicStateException>(() => model.Forecast(state, ForecastHorizon.Observations(3)));
    }

    [Fact]
    public void RequiredStateDimensions_ExposeLogReturnRequirement()
    {
        var model = new AutoregressiveStateModel();

        Assert.Contains(StandardStateDimensions.LogReturn, model.RequiredStateDimensions);
    }
}
