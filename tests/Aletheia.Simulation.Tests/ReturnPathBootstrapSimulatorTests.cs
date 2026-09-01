using Aletheia.Core;
using Aletheia.Simulation;

namespace Aletheia.Simulation.Tests;

public sealed class ReturnPathBootstrapSimulatorTests
{
    [Fact]
    public void HistoricalBootstrap_WithSameSeed_IsDeterministic()
    {
        var horizon = new ForecastHorizonResolver().Resolve(
            ForecastHorizon.Observations(12),
            new DateOnly(2024, 1, 31),
            ObservationFrequency.Monthly);
        var simulator = new ReturnPathBootstrapSimulator();

        var first = simulator.SimulateHistoricalBootstrap([0.01d, -0.02d, 0.005d], horizon, 100, 42);
        var second = simulator.SimulateHistoricalBootstrap([0.01d, -0.02d, 0.005d], horizon, 100, 42);

        Assert.Equal(first.Samples, second.Samples);
        Assert.Equal(first.Distribution.ExpectedReturn, second.Distribution.ExpectedReturn, 12);
    }

    [Fact]
    public void BlockBootstrap_ProducesRequestedSampleCount()
    {
        var horizon = new ForecastHorizonResolver().Resolve(
            ForecastHorizon.Observations(6),
            new DateOnly(2024, 1, 31),
            ObservationFrequency.Monthly);

        var result = new ReturnPathBootstrapSimulator().SimulateBlockBootstrap(
            [0.01d, -0.02d, 0.005d, 0.003d],
            horizon,
            50,
            7,
            2);

        Assert.Equal(50, result.Samples.Count);
        Assert.Equal(ReturnSimulationMethod.BlockBootstrap, result.Method);
    }

    [Fact]
    public void StressScenarioAnalyzer_ReturnsDeterministicStress()
    {
        var result = new StressScenarioAnalyzer().HistoricalWorstWindow([0.02d, -0.10d, -0.05d, 0.01d], 2);

        Assert.True(result.TerminalReturn < 0d);
        Assert.Contains("not a probability", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HistoricalWorstWindow_FromNavSeriesReportsWindowDatesAndPeakDrawdown()
    {
        var series = new NavSeries(
        [
            new NavPoint(new DateOnly(2024, 1, 1), 100m),
            new NavPoint(new DateOnly(2024, 1, 2), 105m),
            new NavPoint(new DateOnly(2024, 1, 3), 102m),
            new NavPoint(new DateOnly(2024, 1, 4), 110m),
            new NavPoint(new DateOnly(2024, 1, 5), 103m),
        ],
        ObservationFrequency.Daily);

        var result = new StressScenarioAnalyzer().HistoricalWorstWindow(series, 3);

        Assert.Equal(new DateOnly(2024, 1, 2), result.StartDate);
        Assert.Equal(new DateOnly(2024, 1, 5), result.EndDate);
        Assert.Equal(3, result.WindowLengthObservations);
        Assert.Equal((103d / 105d) - 1d, result.TerminalReturn, 12);
        Assert.Equal((103d / 110d) - 1d, result.PeakLoss, 12);
        Assert.Contains("minimum terminal return", result.SelectionCriterion, StringComparison.OrdinalIgnoreCase);
    }
}
