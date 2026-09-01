using Aletheia.Core;
using Aletheia.Simulation;

namespace Aletheia.Simulation.Tests;

public sealed class TimingDecisionBacktesterTests
{
    [Fact]
    public void Run_BuyAndHoldStartsInvestedFromFirstObservation()
    {
        var series = CreateSeries(
            ObservationFrequency.Daily,
            (0, 100m),
            (1, 110m),
            (2, 121m));

        var result = new TimingDecisionBacktester().Run(
            series,
            [],
            new TimingBacktestOptions(
                TransactionCostRate: 0d,
                SlippageRate: 0d,
                PeriodsPerYear: 365.25d,
                ChargeInitialFixedExposureCost: false))
            .Single(item => item.StrategyName == "Buy-and-hold");

        Assert.Equal(1d, result.Points[0].Exposure, 12);
        Assert.Equal(1.1d, result.Points[1].PortfolioValue, 12);
        Assert.Equal(1.21d, result.Points[^1].PortfolioValue, 12);
        Assert.Equal(0, result.TradeCount);
        Assert.Equal(1d, result.TimeInMarket, 12);
    }

    [Fact]
    public void Run_BuyAndHoldPaysInitialFixedExposureCostWhenConfigured()
    {
        var series = CreateSeries(
            ObservationFrequency.Daily,
            (0, 100m),
            (1, 100m));

        var result = new TimingDecisionBacktester().Run(
            series,
            [],
            new TimingBacktestOptions(TransactionCostRate: 0.01d, SlippageRate: 0.01d))
            .Single(item => item.StrategyName == "Buy-and-hold");

        Assert.Equal(0.02d, result.Points[0].TradeCost, 12);
        Assert.Equal(0.98d, result.Points[0].PortfolioValue, 12);
        Assert.Equal(0.98d, result.Points[^1].PortfolioValue, 12);
        Assert.Equal(1, result.TradeCount);
        Assert.Equal(1d, result.Turnover, 12);
    }

    [Fact]
    public void Run_NeutralNoActionStaysFlatAndUninvested()
    {
        var series = CreateSeries(
            ObservationFrequency.Daily,
            (0, 100m),
            (1, 110m),
            (2, 121m));

        var result = new TimingDecisionBacktester().Run(
            series,
            [],
            new TimingBacktestOptions(TransactionCostRate: 0d, SlippageRate: 0d))
            .Single(item => item.StrategyName == "Neutral/no-action");

        Assert.All(result.Points, point => Assert.Equal(0d, point.Exposure, 12));
        Assert.Equal(1d, result.Points[^1].PortfolioValue, 12);
        Assert.Equal(0, result.TradeCount);
        Assert.Equal(0d, result.Turnover, 12);
        Assert.Equal(0d, result.TimeInMarket, 12);
    }

    [Fact]
    public void Run_DelaysSignalExecutionUntilNextObservation()
    {
        var start = new DateOnly(2024, 1, 1);
        var series = new NavSeries(
            [
                new NavPoint(start, 100m),
                new NavPoint(start.AddDays(1), 110m),
                new NavPoint(start.AddDays(2), 121m),
            ],
            ObservationFrequency.Daily);
        var signals = new[]
        {
            new TimingBacktestSignal(start, 1d),
        };

        var result = new TimingDecisionBacktester().Run(
            series,
            signals,
            new TimingBacktestOptions(TransactionCostRate: 0d, SlippageRate: 0d, ExecutionDelayObservations: 1, PeriodsPerYear: 365.25d))
            .Single(item => item.StrategyName == "Aletheia timing");

        Assert.Equal(1d, result.Points[1].PortfolioValue, 12);
        Assert.Equal(1d, result.Points[1].Exposure, 12);
        Assert.Equal(1.1d, result.Points[2].PortfolioValue, 12);
        Assert.Equal(start, result.Points[1].SignalDate);
        Assert.Equal(start, result.Points[1].DecisionDate);
        Assert.Equal(start.AddDays(1), result.Points[1].ExecutionDate);
    }

    [Fact]
    public void Run_ChargesCostsOnExposureChanges()
    {
        var start = new DateOnly(2024, 1, 1);
        var series = new NavSeries(
            [
                new NavPoint(start, 100m),
                new NavPoint(start.AddDays(1), 100m),
                new NavPoint(start.AddDays(2), 100m),
            ],
            ObservationFrequency.Daily);
        var signals = new[]
        {
            new TimingBacktestSignal(start, 1d),
        };

        var result = new TimingDecisionBacktester().Run(
            series,
            signals,
            new TimingBacktestOptions(TransactionCostRate: 0.01d, SlippageRate: 0.01d, ExecutionDelayObservations: 1))
            .Single(item => item.StrategyName == "Aletheia timing");

        Assert.Equal(1, result.TradeCount);
        Assert.Equal(1d, result.Turnover, 12);
        Assert.Equal(0.02d, result.Points[1].TradeCost, 12);
        Assert.Equal(0.98d, result.Points[^1].PortfolioValue, 12);
    }

    [Fact]
    public void Run_ClampsSignalExposureToConfiguredMaximum()
    {
        var series = CreateSeries(
            ObservationFrequency.Daily,
            (0, 100m),
            (1, 100m),
            (2, 110m));
        var signals = new[]
        {
            new TimingBacktestSignal(series.StartDate, 2d),
        };

        var result = new TimingDecisionBacktester().Run(
            series,
            signals,
            new TimingBacktestOptions(
                TransactionCostRate: 0d,
                SlippageRate: 0d,
                MaximumGrossExposure: 0.75d))
            .Single(item => item.StrategyName == "Aletheia timing");

        Assert.Equal(0.75d, result.Points[1].Exposure, 12);
        Assert.Equal(1.075d, result.Points[^1].PortfolioValue, 12);
    }

    [Fact]
    public void Run_AllowsLiquidationAndReentryOnDelayedSignals()
    {
        var series = CreateSeries(
            ObservationFrequency.Daily,
            (0, 100m),
            (1, 100m),
            (2, 100m),
            (3, 100m),
            (4, 100m));
        var signals = new[]
        {
            new TimingBacktestSignal(series[0].Date, 1d),
            new TimingBacktestSignal(series[2].Date, 0d),
            new TimingBacktestSignal(series[3].Date, 1d),
        };

        var result = new TimingDecisionBacktester().Run(
            series,
            signals,
            new TimingBacktestOptions(TransactionCostRate: 0d, SlippageRate: 0d))
            .Single(item => item.StrategyName == "Aletheia timing");

        Assert.Equal([0d, 1d, 1d, 0d, 1d], result.Points.Select(point => point.Exposure).ToArray());
        Assert.Equal(3, result.TradeCount);
        Assert.Equal(3d, result.Turnover, 12);
    }

    [Fact]
    public void Run_UsesLastDeterministicSignalWhenSeveralExecuteOnSameObservation()
    {
        var series = CreateSeries(
            ObservationFrequency.Daily,
            (0, 100m),
            (1, 100m),
            (2, 100m));
        var signals = new[]
        {
            new TimingBacktestSignal(series[0].Date, 1d, Source: "A"),
            new TimingBacktestSignal(series[0].Date, 0.25d, Source: "Z"),
        };

        var result = new TimingDecisionBacktester().Run(
            series,
            signals,
            new TimingBacktestOptions(TransactionCostRate: 0d, SlippageRate: 0d))
            .Single(item => item.StrategyName == "Aletheia timing");

        Assert.Equal(0.25d, result.Points[1].Exposure, 12);
        Assert.Equal(1, result.TradeCount);
        Assert.Equal(0.25d, result.Turnover, 12);
    }

    [Fact]
    public void Run_UsesElapsedCalendarTimeForIrregularAnnualizedReturn()
    {
        var start = new DateOnly(2024, 1, 1);
        var series = new NavSeries(
            [
                new NavPoint(start, 100m),
                new NavPoint(start.AddDays(3), 110m),
                new NavPoint(start.AddDays(9), 121m),
            ],
            ObservationFrequency.Irregular);

        var result = new TimingDecisionBacktester().Run(
            series,
            [],
            new TimingBacktestOptions(
                TransactionCostRate: 0d,
                SlippageRate: 0d,
                ChargeInitialFixedExposureCost: false))
            .Single(item => item.StrategyName == "Buy-and-hold");

        Assert.Contains("elapsed calendar-time", result.AnnualizationMethod, StringComparison.Ordinal);
        Assert.Equal(Math.Pow(1.21d, 365.25d / 9d) - 1d, result.AnnualizedReturn, 9);
        Assert.True(result.AnnualizationPeriodsPerYear > 0d);
    }

    [Fact]
    public void Run_UsesExplicitPeriodsPerYearWhenProvided()
    {
        var series = CreateSeries(
            ObservationFrequency.Daily,
            (0, 100m),
            (1, 110m),
            (2, 121m));

        var result = new TimingDecisionBacktester().Run(
            series,
            [],
            new TimingBacktestOptions(
                TransactionCostRate: 0d,
                SlippageRate: 0d,
                PeriodsPerYear: 12d,
                ChargeInitialFixedExposureCost: false))
            .Single(item => item.StrategyName == "Buy-and-hold");

        Assert.Equal("explicit periods-per-year override", result.AnnualizationMethod);
        Assert.Equal(Math.Pow(1.21d, 12d / 2d) - 1d, result.AnnualizedReturn, 12);
    }

    [Fact]
    public void Run_ComputesDrawdownFromNormalizedPortfolioPath()
    {
        var series = CreateSeries(
            ObservationFrequency.Daily,
            (0, 100m),
            (1, 120m),
            (2, 90m),
            (3, 99m));

        var result = new TimingDecisionBacktester().Run(
            series,
            [],
            new TimingBacktestOptions(
                TransactionCostRate: 0d,
                SlippageRate: 0d,
                ChargeInitialFixedExposureCost: false))
            .Single(item => item.StrategyName == "Buy-and-hold");

        Assert.Equal(-0.25d, result.MaximumDrawdown, 12);
        Assert.Equal(-0.01d, result.CumulativeReturn, 12);
    }

    private static NavSeries CreateSeries(
        ObservationFrequency frequency,
        params (int DayOffset, decimal Value)[] values)
    {
        var start = new DateOnly(2024, 1, 1);
        return new NavSeries(
            values.Select(item => new NavPoint(start.AddDays(item.DayOffset), item.Value)),
            frequency);
    }
}
