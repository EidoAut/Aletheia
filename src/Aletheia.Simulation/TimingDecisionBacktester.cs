#pragma warning disable SA1204 // Static helpers are grouped after the backtest workflow.
#pragma warning disable SA1402 // Backtest DTOs are intentionally grouped with the simulator protocol.

using Aletheia.Analytics;
using Aletheia.Core;
using Aletheia.Mathematics;

namespace Aletheia.Simulation;

/// <summary>
/// Simulates economic outcomes from historical out-of-sample timing decisions.
/// </summary>
public sealed class TimingDecisionBacktester
{
    private const double MinimumTradeSize = 1e-12d;

    /// <summary>
    /// Runs Aletheia, buy-and-hold and no-action backtests on the same NAV path.
    /// </summary>
    /// <param name="navSeries">The NAV series.</param>
    /// <param name="signals">Historical out-of-sample timing signals.</param>
    /// <param name="options">Backtest options.</param>
    /// <returns>The comparable backtest results.</returns>
    public IReadOnlyList<TimingBacktestResult> Run(
        NavSeries navSeries,
        IReadOnlyList<TimingBacktestSignal> signals,
        TimingBacktestOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(navSeries);
        ArgumentNullException.ThrowIfNull(signals);
        var effectiveOptions = options ?? new TimingBacktestOptions();
        Validate(navSeries, signals, effectiveOptions);
        return
        [
            this.RunSignalStrategy(navSeries, signals, effectiveOptions),
            this.RunFixedExposure("Buy-and-hold", navSeries, 1d, effectiveOptions, effectiveOptions.ChargeInitialFixedExposureCost),
            this.RunFixedExposure("Neutral/no-action", navSeries, 0d, effectiveOptions, chargeInitialTradeCost: false),
        ];
    }

    private TimingBacktestResult RunSignalStrategy(
        NavSeries navSeries,
        IReadOnlyList<TimingBacktestSignal> signals,
        TimingBacktestOptions options)
    {
        var executionByIndex = BuildExecutionMap(navSeries, signals, options.ExecutionDelayObservations);
        return this.RunPath(
            "Aletheia timing",
            navSeries,
            options,
            initialExposure: 0d,
            chargeInitialTradeCost: false,
            (index, currentExposure) =>
                executionByIndex.TryGetValue(index, out var signal)
                    ? new ExposureDecision(signal.TargetExposure, signal)
                    : new ExposureDecision(currentExposure, null));
    }

    private TimingBacktestResult RunFixedExposure(
        string strategyName,
        NavSeries navSeries,
        double exposure,
        TimingBacktestOptions options,
        bool chargeInitialTradeCost)
    {
        return this.RunPath(
            strategyName,
            navSeries,
            options,
            exposure,
            chargeInitialTradeCost,
            (_, currentExposure) => new ExposureDecision(currentExposure, null));
    }

    private TimingBacktestResult RunPath(
        string strategyName,
        NavSeries navSeries,
        TimingBacktestOptions options,
        double initialExposure,
        bool chargeInitialTradeCost,
        Func<int, double, ExposureDecision> targetExposureAtIndex)
    {
        var value = 1d;
        var exposure = Math.Clamp(initialExposure, 0d, options.MaximumGrossExposure);
        var turnover = 0d;
        var trades = 0;
        var initialTradeCost = 0d;
        if (chargeInitialTradeCost && exposure > MinimumTradeSize)
        {
            initialTradeCost = value * exposure * (options.TransactionCostRate + options.SlippageRate);
            value -= initialTradeCost;
            turnover += exposure;
            trades++;
        }

        var returns = new List<double>(Math.Max(0, navSeries.Count - 1));
        var points = new List<TimingBacktestPoint>(navSeries.Count)
        {
            new(
                navSeries[0].Date,
                value,
                exposure,
                initialTradeCost,
                0d,
                0d,
                null,
                null,
                initialTradeCost > 0d ? navSeries[0].Date : null),
        };

        for (var index = 1; index < navSeries.Count; index++)
        {
            var previousValue = value;
            var assetReturn = ((double)navSeries[index].Value / (double)navSeries[index - 1].Value) - 1d;
            var portfolioReturnBeforeCost = exposure * assetReturn;
            value *= 1d + portfolioReturnBeforeCost;

            // The signal observed at date t is mapped to an execution observation t + delay.
            // The trade is charged after the t -> t+1 return has been realized, so a delayed
            // signal cannot benefit from the return interval that elapsed before execution.
            var decision = targetExposureAtIndex(index, exposure);
            var targetExposure = Math.Clamp(decision.TargetExposure, 0d, options.MaximumGrossExposure);
            var tradeSize = Math.Abs(targetExposure - exposure);
            var tradeCost = value * tradeSize * (options.TransactionCostRate + options.SlippageRate);
            DateOnly? signalDate = null;
            DateOnly? decisionDate = null;
            DateOnly? executionDate = null;
            if (tradeSize > MinimumTradeSize)
            {
                trades++;
                turnover += tradeSize;
                value -= tradeCost;
                exposure = targetExposure;
                signalDate = decision.Signal?.SignalDate;
                decisionDate = decision.Signal?.DecisionDate ?? decision.Signal?.SignalDate;
                executionDate = navSeries[index].Date;
            }

            if (!double.IsFinite(value) || value < 0d)
            {
                throw new InvalidOperationException("Backtest produced a non-finite or negative portfolio value.");
            }

            var realizedReturn = previousValue <= 0d ? 0d : (value / previousValue) - 1d;
            returns.Add(realizedReturn);
            points.Add(new TimingBacktestPoint(
                navSeries[index].Date,
                value,
                exposure,
                tradeCost,
                assetReturn,
                realizedReturn,
                signalDate,
                decisionDate,
                executionDate));
        }

        return BuildResult(strategyName, navSeries, points, returns, turnover, trades, options);
    }

    private static IReadOnlyDictionary<int, TimingBacktestSignal> BuildExecutionMap(
        NavSeries navSeries,
        IReadOnlyList<TimingBacktestSignal> signals,
        int executionDelayObservations)
    {
        var result = new Dictionary<int, TimingBacktestSignal>();
        foreach (var signal in signals
            .OrderBy(signal => signal.SignalDate)
            .ThenBy(signal => signal.DecisionDate ?? signal.SignalDate)
            .ThenBy(signal => signal.Source, StringComparer.Ordinal))
        {
            var signalIndex = FindIndexOnOrAfter(navSeries, signal.SignalDate);
            if (signalIndex < 0)
            {
                continue;
            }

            var executionIndex = signalIndex + executionDelayObservations;
            if (executionIndex > 0 && executionIndex < navSeries.Count)
            {
                result[executionIndex] = signal;
            }
        }

        return result;
    }

    private static TimingBacktestResult BuildResult(
        string strategyName,
        NavSeries navSeries,
        IReadOnlyList<TimingBacktestPoint> points,
        IReadOnlyList<double> returns,
        double turnover,
        int trades,
        TimingBacktestOptions options)
    {
        var annualization = ResolveAnnualization(navSeries, options.PeriodsPerYear);
        var cumulative = points[^1].PortfolioValue - 1d;
        var annualized = ResolveAnnualizedReturn(navSeries, points[^1].PortfolioValue, returns.Count, annualization, options.PeriodsPerYear.HasValue);
        var sampleStandardDeviation = returns.Count < 2 ? 0d : DescriptiveStatistics.SampleStandardDeviation(returns);
        var volatility = sampleStandardDeviation * Math.Sqrt(annualization.PeriodsPerYear);
        var mean = returns.Count == 0 ? 0d : returns.Average();
        var sharpe = sampleStandardDeviation <= 0d ? 0d : (mean / sampleStandardDeviation) * Math.Sqrt(annualization.PeriodsPerYear);
        var downside = returns.Where(value => value < 0d).ToArray();
        var downsideDeviation = downside.Length == 0
            ? 0d
            : Math.Sqrt(downside.Sum(value => value * value) / downside.Length) * Math.Sqrt(annualization.PeriodsPerYear);
        var sortino = downsideDeviation <= 0d ? 0d : (mean * annualization.PeriodsPerYear) / downsideDeviation;
        var maxDrawdown = MaximumDrawdown(points.Select(point => point.PortfolioValue).ToArray());
        var calmar = maxDrawdown >= 0d ? 0d : annualized / Math.Abs(maxDrawdown);
        var timeInMarket = points.Count == 0 ? 0d : points.Count(point => point.Exposure > MinimumTradeSize) / (double)points.Count;
        return new TimingBacktestResult(
            strategyName,
            navSeries.StartDate,
            navSeries.EndDate,
            cumulative,
            annualized,
            volatility,
            sharpe,
            sortino,
            maxDrawdown,
            calmar,
            turnover,
            timeInMarket,
            trades,
            points,
            annualization.PeriodsPerYear,
            annualization.Method);
    }

    private static AnnualizationResolution ResolveAnnualization(NavSeries navSeries, double? explicitPeriodsPerYear)
    {
        if (explicitPeriodsPerYear.HasValue)
        {
            return new AnnualizationResolution(explicitPeriodsPerYear.Value, "explicit periods-per-year override");
        }

        if (navSeries.ObservationFrequency != ObservationFrequency.Irregular)
        {
            var periods = StandardAnnualizationConvention.Default.ResolvePeriodsPerYear(navSeries.ObservationFrequency);
            return new AnnualizationResolution(periods, $"regular {navSeries.ObservationFrequency} convention");
        }

        var elapsed = new ElapsedTimeAnnualizationEstimator().EstimatePeriodsPerYear(navSeries.ToDates());
        return new AnnualizationResolution(elapsed, "elapsed calendar-time convention for irregular observations");
    }

    private static double ResolveAnnualizedReturn(
        NavSeries navSeries,
        double terminalValue,
        int returnCount,
        AnnualizationResolution annualization,
        bool explicitOverride)
    {
        if (returnCount == 0 || terminalValue <= 0d)
        {
            return 0d;
        }

        if (!explicitOverride && navSeries.ObservationFrequency == ObservationFrequency.Irregular)
        {
            var elapsedDays = navSeries.EndDate.DayNumber - navSeries.StartDate.DayNumber;
            if (elapsedDays <= 0)
            {
                return 0d;
            }

            return Math.Pow(terminalValue, 365.25d / elapsedDays) - 1d;
        }

        return Math.Pow(terminalValue, annualization.PeriodsPerYear / returnCount) - 1d;
    }

    private static double MaximumDrawdown(IReadOnlyList<double> values)
    {
        var peak = values[0];
        var drawdown = 0d;
        foreach (var value in values)
        {
            peak = Math.Max(peak, value);
            if (peak > 0d)
            {
                drawdown = Math.Min(drawdown, (value / peak) - 1d);
            }
        }

        return drawdown;
    }

    private static int FindIndexOnOrAfter(NavSeries navSeries, DateOnly date)
    {
        for (var index = 0; index < navSeries.Count; index++)
        {
            if (navSeries[index].Date >= date)
            {
                return index;
            }
        }

        return -1;
    }

    private static void Validate(
        NavSeries navSeries,
        IReadOnlyList<TimingBacktestSignal> signals,
        TimingBacktestOptions options)
    {
        if (navSeries.Count < 2)
        {
            throw new InvalidOperationException("Backtest requires at least two NAV observations.");
        }

        if (options.ExecutionDelayObservations < 1)
        {
            throw new InvalidOperationException("Execution delay must be at least one observation to avoid same-NAV execution.");
        }

        if (!double.IsFinite(options.TransactionCostRate) || options.TransactionCostRate < 0d ||
            !double.IsFinite(options.SlippageRate) || options.SlippageRate < 0d)
        {
            throw new InvalidOperationException("Transaction cost and slippage rates must be finite and non-negative.");
        }

        if (!double.IsFinite(options.MaximumGrossExposure) || options.MaximumGrossExposure <= 0d)
        {
            throw new InvalidOperationException("Maximum gross exposure must be positive and finite.");
        }

        if (options.PeriodsPerYear.HasValue &&
            (!double.IsFinite(options.PeriodsPerYear.Value) || options.PeriodsPerYear.Value <= 0d))
        {
            throw new InvalidOperationException("Periods per year must be positive and finite when supplied.");
        }

        if (signals.Any(signal => !double.IsFinite(signal.TargetExposure)))
        {
            throw new InvalidOperationException("Signal exposure must be finite.");
        }
    }

    private sealed record ExposureDecision(double TargetExposure, TimingBacktestSignal? Signal);

    private sealed record AnnualizationResolution(double PeriodsPerYear, string Method);
}

/// <summary>
/// Stores one historical timing decision supplied to the backtester.
/// </summary>
/// <param name="SignalDate">The date at which the signal was observable.</param>
/// <param name="TargetExposure">The target gross exposure after delayed execution.</param>
/// <param name="Source">The signal source.</param>
/// <param name="CalculationDate">The date on which the historical OOS prediction was calculated.</param>
/// <param name="DecisionDate">The date on which the target exposure decision became available.</param>
public sealed record TimingBacktestSignal(
    DateOnly SignalDate,
    double TargetExposure,
    string Source = "Aletheia",
    DateOnly? CalculationDate = null,
    DateOnly? DecisionDate = null);

/// <summary>
/// Configures timing backtests.
/// </summary>
/// <param name="TransactionCostRate">The proportional trading cost.</param>
/// <param name="SlippageRate">The proportional slippage cost.</param>
/// <param name="ExecutionDelayObservations">The delay between signal observation and execution.</param>
/// <param name="MaximumGrossExposure">The maximum allowed exposure.</param>
/// <param name="PeriodsPerYear">Optional explicit annualization cadence; otherwise the NAV frequency is used.</param>
/// <param name="ChargeInitialFixedExposureCost">Whether fixed-exposure baselines pay the inception trade cost.</param>
public sealed record TimingBacktestOptions(
    double TransactionCostRate = 0.001d,
    double SlippageRate = 0.0005d,
    int ExecutionDelayObservations = 1,
    double MaximumGrossExposure = 1d,
    double? PeriodsPerYear = null,
    bool ChargeInitialFixedExposureCost = true);

/// <summary>
/// Stores one backtest path point.
/// </summary>
/// <param name="Date">The valuation date.</param>
/// <param name="PortfolioValue">The portfolio value normalized to one at inception.</param>
/// <param name="Exposure">The exposure active after this date's execution step.</param>
/// <param name="TradeCost">The cost charged on this date.</param>
/// <param name="AssetReturn">The underlying NAV return since the prior observation.</param>
/// <param name="PortfolioReturn">The net portfolio return since the prior observation.</param>
/// <param name="SignalDate">The signal date for a trade executed on this point, if any.</param>
/// <param name="DecisionDate">The decision date for a trade executed on this point, if any.</param>
/// <param name="ExecutionDate">The execution date for a trade executed on this point, if any.</param>
public sealed record TimingBacktestPoint(
    DateOnly Date,
    double PortfolioValue,
    double Exposure,
    double TradeCost,
    double AssetReturn,
    double PortfolioReturn,
    DateOnly? SignalDate = null,
    DateOnly? DecisionDate = null,
    DateOnly? ExecutionDate = null);

/// <summary>
/// Stores economic backtest metrics.
/// </summary>
/// <param name="StrategyName">The strategy name.</param>
/// <param name="StartDate">The first valuation date.</param>
/// <param name="EndDate">The final valuation date.</param>
/// <param name="CumulativeReturn">The cumulative net return.</param>
/// <param name="AnnualizedReturn">The annualized net return.</param>
/// <param name="AnnualizedVolatility">The annualized volatility.</param>
/// <param name="Sharpe">The zero-rate Sharpe ratio.</param>
/// <param name="Sortino">The zero-rate Sortino ratio.</param>
/// <param name="MaximumDrawdown">The maximum drawdown.</param>
/// <param name="Calmar">The Calmar ratio.</param>
/// <param name="Turnover">Total absolute exposure turnover.</param>
/// <param name="TimeInMarket">Fraction of valuation dates with non-zero exposure.</param>
/// <param name="TradeCount">Number of exposure changes.</param>
/// <param name="Points">The full normalized value path.</param>
/// <param name="AnnualizationPeriodsPerYear">The periods-per-year value used for scaled metrics.</param>
/// <param name="AnnualizationMethod">The annualization method.</param>
public sealed record TimingBacktestResult(
    string StrategyName,
    DateOnly StartDate,
    DateOnly EndDate,
    double CumulativeReturn,
    double AnnualizedReturn,
    double AnnualizedVolatility,
    double Sharpe,
    double Sortino,
    double MaximumDrawdown,
    double Calmar,
    double Turnover,
    double TimeInMarket,
    int TradeCount,
    IReadOnlyList<TimingBacktestPoint> Points,
    double AnnualizationPeriodsPerYear = 0d,
    string AnnualizationMethod = "");
