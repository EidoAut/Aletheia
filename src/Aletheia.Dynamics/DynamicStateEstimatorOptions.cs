using Aletheia.Analytics;

namespace Aletheia.Dynamics;

/// <summary>
/// Configures the initial dynamic-state estimator.
/// </summary>
public sealed record DynamicStateEstimatorOptions
{
    /// <summary>
    /// Gets the trend lookback in observations.
    /// </summary>
    public int TrendLookback { get; init; } = 90;

    /// <summary>
    /// Gets the momentum lookback in observations.
    /// </summary>
    public int MomentumLookback { get; init; } = 30;

    /// <summary>
    /// Gets the realized-volatility lookback in observations.
    /// </summary>
    public int VolatilityLookback { get; init; } = 90;

    /// <summary>
    /// Gets the moving-average window used before numerical differentiation.
    /// </summary>
    public int DerivativeSmoothingWindow { get; init; } = 5;

    /// <summary>
    /// Gets the smoothing method applied before log-NAV differentiation.
    /// </summary>
    public SmoothingMethod DerivativeSmoothingMethod { get; init; } = SmoothingMethod.MovingAverage;

    /// <summary>
    /// Gets the observation count that maps to full state data adequacy.
    /// </summary>
    public int FullDataAdequacyObservationCount { get; init; } = 252;
}
