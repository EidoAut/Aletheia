using Aletheia.Core;
using Aletheia.TimeSeries;

namespace Aletheia.Data;

/// <summary>
/// Normalizes NAV observations for numerical comparison and visualization.
/// </summary>
public sealed class NavSeriesNormalizer
{
    /// <summary>
    /// Converts a NAV series to an indexed value series starting from a base value.
    /// </summary>
    /// <param name="navSeries">The NAV observations.</param>
    /// <param name="baseValue">The normalized first value.</param>
    /// <returns>A normalized double-valued time series.</returns>
    public TimeSeries<double> NormalizeToBase(NavSeries navSeries, double baseValue = 100d)
    {
        ArgumentNullException.ThrowIfNull(navSeries);

        if (baseValue <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(baseValue), baseValue, "Base value must be positive.");
        }

        if (navSeries.Count == 0)
        {
            return new TimeSeries<double>(Array.Empty<TimeSeriesPoint<double>>(), navSeries.ObservationFrequency);
        }

        var firstValue = navSeries[0].Value;
        if (firstValue <= 0m)
        {
            throw new ArgumentException("Normalization requires a positive first NAV value.", nameof(navSeries));
        }

        var points = new List<TimeSeriesPoint<double>>(navSeries.Count);
        for (var index = 0; index < navSeries.Count; index++)
        {
            var normalizedValue = ((double)navSeries[index].Value / (double)firstValue) * baseValue;
            points.Add(new TimeSeriesPoint<double>(navSeries[index].Date, normalizedValue));
        }

        return new TimeSeries<double>(points, navSeries.ObservationFrequency);
    }
}
