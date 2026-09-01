namespace Aletheia.Analytics;

/// <summary>
/// Identifies the smoothing method used before noise-sensitive calculations.
/// </summary>
public enum SmoothingMethod
{
    /// <summary>
    /// No smoothing is applied.
    /// </summary>
    None,

    /// <summary>
    /// A trailing simple moving average is applied.
    /// </summary>
    MovingAverage,
}
