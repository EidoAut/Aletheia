namespace Aletheia.Validation;

/// <summary>
/// Classifies forecast and realized returns for directional-accuracy calculations.
/// </summary>
public enum ForecastDirection
{
    /// <summary>
    /// The return is materially below zero.
    /// </summary>
    Negative = -1,

    /// <summary>
    /// The return is within the configured zero tolerance.
    /// </summary>
    Flat = 0,

    /// <summary>
    /// The return is materially above zero.
    /// </summary>
    Positive = 1,
}
