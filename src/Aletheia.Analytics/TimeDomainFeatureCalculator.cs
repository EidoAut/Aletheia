using Aletheia.Core;
using Aletheia.Mathematics;
using Aletheia.TimeSeries;

namespace Aletheia.Analytics;

/// <summary>
/// Extracts initial time-domain features used by dynamic-state reconstruction.
/// </summary>
public sealed class TimeDomainFeatureCalculator
{
    /// <summary>
    /// Calculates a moving average over a double-valued series.
    /// </summary>
    /// <param name="series">The input series.</param>
    /// <param name="windowSize">The number of observations in each window.</param>
    /// <returns>A moving-average series dated at each window end.</returns>
    public TimeSeries<double> CalculateMovingAverage(TimeSeries<double> series, int windowSize)
    {
        ArgumentNullException.ThrowIfNull(series);

        if (windowSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowSize), windowSize, "Window size must be positive.");
        }

        var points = new List<TimeSeriesPoint<double>>();
        foreach (var window in series.RollingWindows(windowSize))
        {
            points.Add(new TimeSeriesPoint<double>(window.EndDate, DescriptiveStatistics.Mean(window.ToValueArray())));
        }

        return new TimeSeries<double>(points, series.ObservationFrequency);
    }

    /// <summary>
    /// Estimates first-order trend using a linear regression slope over recent log NAV values.
    /// </summary>
    /// <param name="navSeries">The NAV observations.</param>
    /// <param name="lookback">The number of trailing observations to use.</param>
    /// <returns>The fitted slope in log-NAV units per observation.</returns>
    public double CalculateFirstOrderTrend(NavSeries navSeries, int lookback)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        if (lookback < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(lookback), lookback, "Trend lookback must be at least two observations.");
        }

        var count = Math.Min(lookback, navSeries.Count);
        if (count < 2)
        {
            return 0d;
        }

        var x = new double[count];
        var y = new double[count];
        var start = navSeries.Count - count;

        for (var index = 0; index < count; index++)
        {
            var value = navSeries[start + index].Value;
            if (value <= 0m)
            {
                throw new ArgumentException("Trend calculation requires positive NAV values.", nameof(navSeries));
            }

            x[index] = index;
            y[index] = Math.Log((double)value);
        }

        return LinearRegression.Fit(x, y).Slope;
    }

    /// <summary>
    /// Calculates simple price momentum over a trailing observation window.
    /// </summary>
    /// <param name="navSeries">The NAV observations.</param>
    /// <param name="lookback">The trailing lookback in observations.</param>
    /// <returns>The trailing simple return.</returns>
    public double CalculateMomentum(NavSeries navSeries, int lookback)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        if (lookback <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lookback), lookback, "Momentum lookback must be positive.");
        }

        if (navSeries.Count <= lookback)
        {
            return 0d;
        }

        var start = navSeries[navSeries.Count - lookback - 1].Value;
        var end = navSeries[navSeries.Count - 1].Value;
        if (start <= 0m || end <= 0m)
        {
            throw new ArgumentException("Momentum calculation requires positive NAV values.", nameof(navSeries));
        }

        return ((double)end / (double)start) - 1d;
    }

    /// <summary>
    /// Calculates sample autocorrelation at a positive lag.
    /// </summary>
    /// <param name="series">The input series.</param>
    /// <param name="lag">The positive lag in observations.</param>
    /// <returns>The lagged autocorrelation, or 0 when variance is zero.</returns>
    public double CalculateAutocorrelation(TimeSeries<double> series, int lag)
    {
        ArgumentNullException.ThrowIfNull(series);

        if (lag <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lag), lag, "Lag must be positive.");
        }

        if (series.Count <= lag)
        {
            return 0d;
        }

        var values = series.ToValueArray();
        var x = new double[values.Length - lag];
        var y = new double[values.Length - lag];

        for (var index = lag; index < values.Length; index++)
        {
            x[index - lag] = values[index];
            y[index - lag] = values[index - lag];
        }

        return DescriptiveStatistics.Correlation(x, y);
    }
}
