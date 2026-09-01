using Aletheia.Core;
using Aletheia.Simulation;

namespace Aletheia.Simulation.Tests;

public sealed class InvestmentPlanSimulatorTests
{
    [Fact]
    public void Simulate_WithDeterministicSeed_ReturnsRepeatableTrajectory()
    {
        var options = new InvestmentPlanOptions
        {
            InitialInvestment = 1_800d,
            MonthlyContribution = 100d,
            HorizonMonths = 12,
            PathCount = 250,
            Seed = 42,
        };
        var first = new InvestmentPlanSimulator(options).Simulate(
            [0.01d, -0.004d, 0.002d, 0.005d],
            ObservationFrequency.Monthly,
            new DateOnly(2024, 1, 31));
        var second = new InvestmentPlanSimulator(options).Simulate(
            [0.01d, -0.004d, 0.002d, 0.005d],
            ObservationFrequency.Monthly,
            new DateOnly(2024, 1, 31));

        Assert.Equal(first.Trajectory, second.Trajectory);
        Assert.Equal(first.MedianTerminalValue, second.MedianTerminalValue);
        Assert.Equal(13, first.Trajectory.Count);
    }

    [Fact]
    public void Simulate_WithZeroReturns_EndsAtTotalContributions()
    {
        var options = new InvestmentPlanOptions
        {
            InitialInvestment = 1_000d,
            MonthlyContribution = 100d,
            HorizonMonths = 12,
            PathCount = 100,
            Seed = 7,
        };

        var result = new InvestmentPlanSimulator(options).Simulate(
            [0d, 0d],
            ObservationFrequency.Monthly,
            new DateOnly(2024, 1, 31));

        Assert.Equal(2_200d, result.TotalContributed, 10);
        Assert.Equal(2_200d, result.MedianTerminalValue, 10);
        Assert.Equal(0d, result.ProbabilityTerminalBelowContributions, 10);
    }

    [Fact]
    public void Simulate_WithBusinessDailyReturns_ExposesMonthlyScaledMoments()
    {
        var options = new InvestmentPlanOptions
        {
            InitialInvestment = 1_000d,
            MonthlyContribution = 0d,
            HorizonMonths = 1,
            PathCount = 100,
            Seed = 7,
        };

        var result = new InvestmentPlanSimulator(options).Simulate(
            [0.001d, 0.001d],
            ObservationFrequency.BusinessDaily,
            new DateOnly(2024, 1, 31));

        Assert.Equal(21d, result.ObservationPeriodsPerMonth, 10);
        Assert.Equal(0.021d, result.MonthlyMeanLogReturn, 10);
        Assert.Equal(0d, result.MonthlyStandardDeviation, 10);
    }

    [Fact]
    public void Simulate_WithInsufficientHistoricalReturns_Throws()
    {
        var options = new InvestmentPlanOptions { HorizonMonths = 12, PathCount = 100 };
        var simulator = new InvestmentPlanSimulator(options);

        var exception = Assert.Throws<InvalidOperationException>(() => simulator.Simulate(
            [0.01d],
            ObservationFrequency.Monthly,
            new DateOnly(2024, 1, 31)));

        Assert.Contains("at least two", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Simulate_WithNonFiniteHistoricalReturn_Throws()
    {
        var options = new InvestmentPlanOptions { HorizonMonths = 12, PathCount = 100 };
        var simulator = new InvestmentPlanSimulator(options);

        var exception = Assert.Throws<InvalidOperationException>(() => simulator.Simulate(
            [0.01d, double.NaN],
            ObservationFrequency.Monthly,
            new DateOnly(2024, 1, 31)));

        Assert.Contains("finite", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Simulate_WithNonFiniteTotalContributions_Throws()
    {
        var options = new InvestmentPlanOptions
        {
            InitialInvestment = double.MaxValue,
            MonthlyContribution = double.MaxValue,
            HorizonMonths = 12,
            PathCount = 100,
        };
        var simulator = new InvestmentPlanSimulator(options);

        var exception = Assert.Throws<InvalidOperationException>(() => simulator.Simulate(
            [0.01d, -0.01d],
            ObservationFrequency.Monthly,
            new DateOnly(2024, 1, 31)));

        Assert.Contains("contributed capital", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Simulate_WithExcessivePathMonthWorkload_Throws()
    {
        var options = new InvestmentPlanOptions
        {
            HorizonMonths = 600,
            PathCount = 100_000,
        };
        var simulator = new InvestmentPlanSimulator(options);

        var exception = Assert.Throws<InvalidOperationException>(() => simulator.Simulate(
            [0.01d, -0.01d],
            ObservationFrequency.Monthly,
            new DateOnly(2024, 1, 31)));

        Assert.Contains("path-months", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Simulate_WithIrregularFrequencyAndExplicitCadence_ScalesMoments()
    {
        var options = new InvestmentPlanOptions
        {
            InitialInvestment = 1_000d,
            MonthlyContribution = 0d,
            HorizonMonths = 1,
            PathCount = 100,
            Seed = 7,
        };

        var result = new InvestmentPlanSimulator(options).Simulate(
            [0.001d, 0.001d],
            ObservationFrequency.Irregular,
            new DateOnly(2024, 1, 31),
            periodsPerYear: 24d);

        Assert.Equal(2d, result.ObservationPeriodsPerMonth, 10);
        Assert.Equal(0.002d, result.MonthlyMeanLogReturn, 10);
        Assert.Contains("irregular cadence", result.Methodology, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Simulate_WithIrregularFrequency_Throws()
    {
        var options = new InvestmentPlanOptions { HorizonMonths = 12, PathCount = 100 };
        var simulator = new InvestmentPlanSimulator(options);

        var exception = Assert.Throws<InvalidOperationException>(() => simulator.Simulate(
            [0.01d, -0.01d],
            ObservationFrequency.Irregular,
            new DateOnly(2024, 1, 31)));

        Assert.Contains("regular observation frequency", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Simulate_WithExternalCostsAndInflation_SeparatesNominalAndRealTerminalValues()
    {
        var options = new InvestmentPlanOptions
        {
            InitialInvestment = 1_000d,
            MonthlyContribution = 100d,
            HorizonMonths = 12,
            PathCount = 100,
            Seed = 7,
            EntryFeeRate = 0.01d,
            ExitFeeRate = 0.02d,
            AnnualServiceCostRate = 0.01d,
            AnnualInflationRate = 0.03d,
        };

        var result = new InvestmentPlanSimulator(options).Simulate(
            [0d, 0d],
            ObservationFrequency.Monthly,
            new DateOnly(2024, 1, 31));

        Assert.True(result.MedianTerminalValue < result.TotalContributed);
        Assert.True(result.MedianRealTerminalValue < result.MedianTerminalValue);
        Assert.InRange(result.ProbabilityTerminalBelowContributions, 0d, 1d);
        Assert.Contains("external investor costs", result.Methodology, StringComparison.OrdinalIgnoreCase);
    }
}
