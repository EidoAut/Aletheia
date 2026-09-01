using Aletheia.Core;
using Aletheia.Simulation;

namespace Aletheia.Simulation.Tests;

public sealed class MonteCarloSimulatorTests
{
    [Fact]
    public void Simulate_WithDeterministicSeed_ReturnsRepeatableSamples()
    {
        var options = new MonteCarloOptions { PathCount = 10, Seed = 42 };
        var resolution = new ForecastHorizonResolution(
            ForecastHorizon.Observations(7),
            ObservationFrequency.BusinessDaily,
            7,
            null,
            "UnitTestPolicy",
            false);
        var first = new MonteCarloSimulator(options).Simulate([0.01d, -0.005d, 0.002d], resolution);
        var second = new MonteCarloSimulator(options).Simulate([0.01d, -0.005d, 0.002d], resolution);

        Assert.Equal(first.Samples, second.Samples);
        Assert.Equal(10, first.Samples.Count);
        Assert.Equal(7, first.SimulationStepCount);
    }

    [Fact]
    public void Simulate_WithCalendarDayHorizon_UsesResolvedBusinessObservationSteps()
    {
        var options = new MonteCarloOptions { PathCount = 2, Seed = 42 };
        var simulator = new MonteCarloSimulator(options);

        var result = simulator.Simulate(
            [0.01d, -0.005d, 0.002d],
            ForecastHorizon.CalendarDays(3),
            ObservationFrequency.BusinessDaily,
            new DateOnly(2024, 1, 5));

        Assert.Equal(new DateOnly(2024, 1, 8), result.HorizonResolution.TargetDate);
        Assert.Equal(1, result.SimulationStepCount);
    }
}
