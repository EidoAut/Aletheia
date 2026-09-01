using Aletheia.Core;
using Aletheia.TimeSeries;

namespace Aletheia.Analytics;

/// <summary>
/// Calculates simple, logarithmic, rolling, cumulative, and annualized returns.
/// </summary>
public sealed class ReturnCalculator
{
    private const double DaysPerYear = 365.25d;

    /// <summary>
    /// Calculates simple returns from a NAV series.
    /// </summary>
    /// <remarks>
    /// Simple return is defined as <c>R_t = (P_t - P_{t-1}) / P_{t-1}</c>.
    /// </remarks>
    /// <param name="navSeries">The NAV observations.</param>
    /// <returns>A time series of simple returns dated at <c>t</c>.</returns>
    public TimeSeries<double> CalculateSimpleReturns(NavSeries navSeries)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        if (navSeries.Count < 2)
        {
            return new TimeSeries<double>(
                Array.Empty<TimeSeriesPoint<double>>(),
                navSeries.ObservationFrequency);
        }

        var returns = new List<TimeSeriesPoint<double>>(navSeries.Count - 1);
        for (var index = 1; index < navSeries.Count; index++)
        {
            var previous = EnsurePositive(navSeries[index - 1].Value, nameof(navSeries));
            var current = EnsurePositive(navSeries[index].Value, nameof(navSeries));
            var value = ((double)current - (double)previous) / (double)previous;
            returns.Add(new TimeSeriesPoint<double>(navSeries[index].Date, value));
        }

        return new TimeSeries<double>(returns, navSeries.ObservationFrequency);
    }

    /// <summary>
    /// Calculates logarithmic returns from a NAV series.
    /// </summary>
    /// <remarks>
    /// Logarithmic return is defined as <c>r_t = ln(P_t / P_{t-1})</c>.
    /// Consecutive log returns are additive over time, which is useful for
    /// forecast aggregation and Monte Carlo simulation.
    /// </remarks>
    /// <param name="navSeries">The NAV observations.</param>
    /// <returns>A time series of logarithmic returns dated at <c>t</c>.</returns>
    public TimeSeries<double> CalculateLogReturns(NavSeries navSeries)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        if (navSeries.Count < 2)
        {
            return new TimeSeries<double>(
                Array.Empty<TimeSeriesPoint<double>>(),
                navSeries.ObservationFrequency);
        }

        var returns = new List<TimeSeriesPoint<double>>(navSeries.Count - 1);
        for (var index = 1; index < navSeries.Count; index++)
        {
            var previous = EnsurePositive(navSeries[index - 1].Value, nameof(navSeries));
            var current = EnsurePositive(navSeries[index].Value, nameof(navSeries));
            var value = Math.Log((double)current / (double)previous);
            returns.Add(new TimeSeriesPoint<double>(navSeries[index].Date, value));
        }

        return new TimeSeries<double>(returns, navSeries.ObservationFrequency);
    }

    /// <summary>
    /// Calculates rolling simple returns over a fixed observation window.
    /// </summary>
    /// <param name="navSeries">The NAV observations.</param>
    /// <param name="windowSize">The number of observations between entry and exit.</param>
    /// <returns>Rolling simple returns dated at the exit observation.</returns>
    public TimeSeries<double> CalculateRollingReturns(NavSeries navSeries, int windowSize)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        if (windowSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowSize), windowSize, "Window size must be positive.");
        }

        if (navSeries.Count <= windowSize)
        {
            return new TimeSeries<double>(
                Array.Empty<TimeSeriesPoint<double>>(),
                navSeries.ObservationFrequency);
        }

        var returns = new List<TimeSeriesPoint<double>>(navSeries.Count - windowSize);
        for (var index = windowSize; index < navSeries.Count; index++)
        {
            var start = EnsurePositive(navSeries[index - windowSize].Value, nameof(navSeries));
            var end = EnsurePositive(navSeries[index].Value, nameof(navSeries));
            returns.Add(new TimeSeriesPoint<double>(navSeries[index].Date, ((double)end / (double)start) - 1d));
        }

        return new TimeSeries<double>(returns, navSeries.ObservationFrequency);
    }

    /// <summary>
    /// Calculates total cumulative return from the first to the last observation.
    /// </summary>
    /// <param name="navSeries">The NAV observations.</param>
    /// <returns>Total return over the available history.</returns>
    public double CalculateCumulativeReturn(NavSeries navSeries)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        if (navSeries.Count < 2)
        {
            return 0d;
        }

        var first = EnsurePositive(navSeries[0].Value, nameof(navSeries));
        var last = EnsurePositive(navSeries[navSeries.Count - 1].Value, nameof(navSeries));

        return ((double)last / (double)first) - 1d;
    }

    /// <summary>
    /// Calculates compound annual growth rate.
    /// </summary>
    /// <param name="navSeries">The NAV observations.</param>
    /// <returns>The annualized geometric return.</returns>
    public double CalculateCagr(NavSeries navSeries)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        if (navSeries.Count < 2)
        {
            return 0d;
        }

        var first = EnsurePositive(navSeries[0].Value, nameof(navSeries));
        var last = EnsurePositive(navSeries[navSeries.Count - 1].Value, nameof(navSeries));
        var elapsedDays = navSeries.EndDate.DayNumber - navSeries.StartDate.DayNumber;
        if (elapsedDays <= 0)
        {
            return 0d;
        }

        var years = elapsedDays / DaysPerYear;
        return Math.Pow((double)last / (double)first, 1d / years) - 1d;
    }

    private static decimal EnsurePositive(decimal value, string parameterName)
    {
        if (value <= 0m)
        {
            throw new ArgumentException("Return calculations require strictly positive NAV values.", parameterName);
        }

        return value;
    }
}
