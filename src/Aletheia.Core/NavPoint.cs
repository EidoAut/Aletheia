namespace Aletheia.Core;

/// <summary>
/// Represents one dated net asset value observation.
/// </summary>
/// <remarks>
/// The value is stored as <see cref="decimal"/> because NAV observations are
/// financial quantities. Numerical algorithms convert to <see cref="double"/>
/// only after data quality checks and normalization.
/// </remarks>
public readonly record struct NavPoint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NavPoint"/> struct.
    /// </summary>
    /// <param name="date">The observation date.</param>
    /// <param name="value">The observed NAV value.</param>
    public NavPoint(DateOnly date, decimal value)
    {
        this.Date = date;
        this.Value = value;
    }

    /// <summary>
    /// Gets the observation date.
    /// </summary>
    public DateOnly Date { get; }

    /// <summary>
    /// Gets the observed NAV.
    /// </summary>
    public decimal Value { get; }
}
