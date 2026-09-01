namespace Aletheia.TimeSeries;

/// <summary>
/// Represents one dated observation in a generic time series.
/// </summary>
/// <typeparam name="T">The observation value type.</typeparam>
public readonly record struct TimeSeriesPoint<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeSeriesPoint{T}"/> struct.
    /// </summary>
    /// <param name="date">The observation date.</param>
    /// <param name="value">The observation value.</param>
    public TimeSeriesPoint(DateOnly date, T value)
    {
        this.Date = date;
        this.Value = value;
    }

    /// <summary>
    /// Gets the observation date.
    /// </summary>
    public DateOnly Date { get; }

    /// <summary>
    /// Gets the observation value.
    /// </summary>
    public T Value { get; }
}
