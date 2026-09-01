namespace Aletheia.Data;

/// <summary>
/// Configures NAV data-quality diagnostics.
/// </summary>
public sealed record DataQualityOptions
{
    /// <summary>
    /// Gets the minimum observation count before the series is considered sufficient.
    /// </summary>
    public int MinimumObservationCount { get; init; } = 252;

    /// <summary>
    /// Gets the number of calendar days above which an adjacent date gap is large.
    /// </summary>
    public int LargeGapThresholdDays { get; init; } = 10;

    /// <summary>
    /// Gets the absolute log-return threshold used to flag suspicious jumps.
    /// </summary>
    public double SuspiciousJumpLogReturnThreshold { get; init; } = Math.Log(1.25d);

    /// <summary>
    /// Gets the number of repeated values considered a stale streak.
    /// </summary>
    public int StaleRunThreshold { get; init; } = 5;
}
