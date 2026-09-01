using Aletheia.Core;

namespace Aletheia.TimeSeries;

/// <summary>
/// Numerical convenience methods for double-valued time series.
/// </summary>
public static class DoubleTimeSeriesExtensions
{
    /// <summary>
    /// Creates a double-valued series from aligned dates and values.
    /// </summary>
    /// <param name="dates">The observation dates.</param>
    /// <param name="values">The observation values.</param>
    /// <param name="observationFrequency">The declared observation frequency.</param>
    /// <returns>A validated double-valued time series.</returns>
    public static TimeSeries<double> FromAlignedArrays(
        DateOnly[] dates,
        double[] values,
        ObservationFrequency observationFrequency = ObservationFrequency.Irregular)
    {
        ArgumentNullException.ThrowIfNull(dates);
        ArgumentNullException.ThrowIfNull(values);

        if (dates.Length != values.Length)
        {
            throw new ArgumentException("Dates and values must have the same length.", nameof(values));
        }

        var points = new TimeSeriesPoint<double>[dates.Length];
        for (var index = 0; index < dates.Length; index++)
        {
            points[index] = new TimeSeriesPoint<double>(dates[index], values[index]);
        }

        return new TimeSeries<double>(points, observationFrequency);
    }
}
