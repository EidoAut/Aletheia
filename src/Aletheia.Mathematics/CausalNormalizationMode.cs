namespace Aletheia.Mathematics;

/// <summary>
/// Specifies how a time-ordered sequence is normalized without future leakage.
/// </summary>
public enum CausalNormalizationMode
{
    /// <summary>
    /// Uses all observations available up to and including the current one.
    /// </summary>
    ExpandingZScore,

    /// <summary>
    /// Uses a trailing fixed-width observation window ending at the current one.
    /// </summary>
    RollingZScore,

    /// <summary>
    /// Uses the trailing median and median absolute deviation ending at the current one.
    /// </summary>
    RollingRobust,
}
