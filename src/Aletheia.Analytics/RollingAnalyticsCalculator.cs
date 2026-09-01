using Aletheia.Core;
using Aletheia.Mathematics;
using Aletheia.TimeSeries;

namespace Aletheia.Analytics;

/// <summary>
/// Calculates causal rolling analytics dated at each window end.
/// </summary>
public sealed class RollingAnalyticsCalculator
{
    private readonly ReturnCalculator returnCalculator;
    private readonly RiskMetricsCalculator riskMetricsCalculator;

    /// <summary>
    /// Initializes a new instance of the <see cref="RollingAnalyticsCalculator"/> class.
    /// </summary>
    /// <param name="returnCalculator">The return calculator.</param>
    /// <param name="riskMetricsCalculator">The risk calculator.</param>
    public RollingAnalyticsCalculator(
        ReturnCalculator? returnCalculator = null,
        RiskMetricsCalculator? riskMetricsCalculator = null)
    {
        this.returnCalculator = returnCalculator ?? new ReturnCalculator();
        this.riskMetricsCalculator = riskMetricsCalculator ?? new RiskMetricsCalculator();
    }

    /// <summary>
    /// Calculates rolling simple returns from a NAV series.
    /// </summary>
    /// <param name="navSeries">The NAV observations.</param>
    /// <param name="windowSize">The rolling window size.</param>
    /// <returns>Rolling simple returns.</returns>
    public TimeSeries<double> RollingReturn(NavSeries navSeries, int windowSize)
    {
        return this.returnCalculator.CalculateRollingReturns(navSeries, windowSize);
    }

    /// <summary>
    /// Calculates rolling annualized volatility.
    /// </summary>
    /// <param name="returns">The periodic return series.</param>
    /// <param name="windowSize">The rolling window size.</param>
    /// <param name="periodsPerYear">The optional annualization factor.</param>
    /// <returns>Rolling annualized volatility.</returns>
    public TimeSeries<double> RollingVolatility(
        TimeSeries<double> returns,
        int windowSize,
        double? periodsPerYear = null)
    {
        return this.riskMetricsCalculator.CalculateRollingVolatility(returns, windowSize, periodsPerYear);
    }

    /// <summary>
    /// Calculates rolling Sharpe ratio.
    /// </summary>
    /// <param name="returns">The periodic return series.</param>
    /// <param name="windowSize">The rolling window size.</param>
    /// <param name="annualRiskFreeRate">The annual risk-free rate.</param>
    /// <param name="periodsPerYear">The optional annualization factor.</param>
    /// <returns>Rolling Sharpe ratios.</returns>
    public TimeSeries<double> RollingSharpe(
        TimeSeries<double> returns,
        int windowSize,
        double annualRiskFreeRate = 0d,
        double? periodsPerYear = null)
    {
        return this.RollingReturnMetric(
            returns,
            windowSize,
            window => this.riskMetricsCalculator.CalculateSharpeRatio(window, annualRiskFreeRate, periodsPerYear));
    }

    /// <summary>
    /// Calculates rolling Sortino ratio.
    /// </summary>
    /// <param name="returns">The periodic return series.</param>
    /// <param name="windowSize">The rolling window size.</param>
    /// <param name="annualTargetReturn">The annual target return.</param>
    /// <param name="periodsPerYear">The optional annualization factor.</param>
    /// <returns>Rolling Sortino ratios.</returns>
    public TimeSeries<double> RollingSortino(
        TimeSeries<double> returns,
        int windowSize,
        double annualTargetReturn = 0d,
        double? periodsPerYear = null)
    {
        return this.RollingReturnMetric(
            returns,
            windowSize,
            window => this.riskMetricsCalculator.CalculateSortinoRatio(window, annualTargetReturn, periodsPerYear));
    }

    /// <summary>
    /// Calculates rolling maximum drawdown.
    /// </summary>
    /// <param name="navSeries">The NAV observations.</param>
    /// <param name="windowSize">The rolling window size.</param>
    /// <returns>Rolling maximum drawdown.</returns>
    public TimeSeries<double> RollingDrawdown(NavSeries navSeries, int windowSize)
    {
        ArgumentNullException.ThrowIfNull(navSeries);
        ValidateWindow(windowSize);

        if (navSeries.Count < windowSize)
        {
            return Empty(navSeries.ObservationFrequency);
        }

        var points = new List<TimeSeriesPoint<double>>(navSeries.Count - windowSize + 1);
        for (var start = 0; start <= navSeries.Count - windowSize; start++)
        {
            var window = new NavSeries(navSeries.Points.Skip(start).Take(windowSize), navSeries.ObservationFrequency);
            var drawdown = this.riskMetricsCalculator.CalculateMaximumDrawdown(window);
            points.Add(new TimeSeriesPoint<double>(window.EndDate, drawdown.MaximumDrawdown));
        }

        return new TimeSeries<double>(points, navSeries.ObservationFrequency);
    }

    /// <summary>
    /// Calculates rolling autocorrelation.
    /// </summary>
    /// <param name="returns">The periodic return series.</param>
    /// <param name="windowSize">The rolling window size.</param>
    /// <param name="lag">The autocorrelation lag.</param>
    /// <returns>Rolling autocorrelation.</returns>
    public TimeSeries<double> RollingAutocorrelation(TimeSeries<double> returns, int windowSize, int lag = 1)
    {
        return this.RollingValueMetric(
            returns,
            windowSize,
            values => values.Length <= lag ? 0d : DescriptiveStatistics.Autocorrelation(values, lag));
    }

    /// <summary>
    /// Calculates rolling sample skewness.
    /// </summary>
    /// <param name="returns">The periodic return series.</param>
    /// <param name="windowSize">The rolling window size.</param>
    /// <returns>Rolling skewness.</returns>
    public TimeSeries<double> RollingSkewness(TimeSeries<double> returns, int windowSize)
    {
        return this.RollingValueMetric(
            returns,
            windowSize,
            values => values.Length < 3 ? 0d : DescriptiveStatistics.Skewness(values));
    }

    /// <summary>
    /// Calculates rolling excess kurtosis.
    /// </summary>
    /// <param name="returns">The periodic return series.</param>
    /// <param name="windowSize">The rolling window size.</param>
    /// <returns>Rolling excess kurtosis.</returns>
    public TimeSeries<double> RollingKurtosis(TimeSeries<double> returns, int windowSize)
    {
        return this.RollingValueMetric(
            returns,
            windowSize,
            values => values.Length < 4 ? 0d : DescriptiveStatistics.ExcessKurtosis(values));
    }

    private static TimeSeries<double> Empty(ObservationFrequency frequency)
    {
        return new TimeSeries<double>(Array.Empty<TimeSeriesPoint<double>>(), frequency);
    }

    private static void ValidateWindow(int windowSize)
    {
        if (windowSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowSize), windowSize, "Window size must be positive.");
        }
    }

    private TimeSeries<double> RollingReturnMetric(
        TimeSeries<double> returns,
        int windowSize,
        Func<TimeSeries<double>, double> metric)
    {
        ArgumentNullException.ThrowIfNull(returns);
        ArgumentNullException.ThrowIfNull(metric);
        ValidateWindow(windowSize);

        var points = new List<TimeSeriesPoint<double>>();
        foreach (var window in returns.RollingWindows(windowSize))
        {
            points.Add(new TimeSeriesPoint<double>(window.EndDate, metric(window)));
        }

        return new TimeSeries<double>(points, returns.ObservationFrequency);
    }

    private TimeSeries<double> RollingValueMetric(
        TimeSeries<double> returns,
        int windowSize,
        Func<double[], double> metric)
    {
        return this.RollingReturnMetric(
            returns,
            windowSize,
            window => metric(window.ToValueArray()));
    }
}
