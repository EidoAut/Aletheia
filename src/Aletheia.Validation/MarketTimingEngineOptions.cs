using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Centralizes market-timing thresholds and validation settings.
/// </summary>
public sealed record MarketTimingEngineOptions
{
    /// <summary>
    /// Gets the default event horizons.
    /// </summary>
    public IReadOnlyList<ForecastHorizon> Horizons { get; init; } =
    [
        ForecastHorizon.Observations(5),
        ForecastHorizon.Observations(10),
        ForecastHorizon.Observations(20),
        ForecastHorizon.Observations(60),
        ForecastHorizon.Observations(120),
    ];

    /// <summary>
    /// Gets the fixed upside threshold.
    /// </summary>
    public double UpsideThreshold { get; init; } = 0.03d;

    /// <summary>
    /// Gets the fixed downside threshold.
    /// </summary>
    public double DownsideThreshold { get; init; } = 0.03d;

    /// <summary>
    /// Gets the barrier policy.
    /// </summary>
    public BarrierThresholdPolicy BarrierPolicy { get; init; } = BarrierThresholdPolicy.VolatilityScaled;

    /// <summary>
    /// Gets the upside volatility multiplier.
    /// </summary>
    public double UpsideVolatilityMultiplier { get; init; } = 3d;

    /// <summary>
    /// Gets the downside volatility multiplier.
    /// </summary>
    public double DownsideVolatilityMultiplier { get; init; } = 3d;

    /// <summary>
    /// Gets a value indicating whether GARCH, Kalman, and HMM state features are fitted.
    /// </summary>
    public bool EnableStateModelFeatures { get; init; } = true;

    /// <summary>
    /// Gets the maximum HMM EM iterations used for state-model timing features.
    /// </summary>
    public int HmmMaximumIterations { get; init; } = 100;

    /// <summary>
    /// Gets the minimum causal feature index.
    /// </summary>
    public int MinimumFeatureIndex { get; init; } = 120;

    /// <summary>
    /// Gets the minimum training samples.
    /// </summary>
    public int MinimumTrainingSamples { get; init; } = 80;

    /// <summary>
    /// Gets the purging observations added to the horizon.
    /// </summary>
    public int PurgeObservations { get; init; } = 0;

    /// <summary>
    /// Gets the embargo observations after each validation target.
    /// </summary>
    public int EmbargoObservations { get; init; } = 5;

    /// <summary>
    /// Gets the maximum walk-forward evaluation points per horizon.
    /// </summary>
    public int MaximumWalkForwardEvaluations { get; init; } = 64;

    /// <summary>
    /// Gets the absolute minimum OOS samples required before a model can be considered.
    /// Unit: walk-forward predictions for the same horizon.
    /// </summary>
    public int MinimumOosSamplesAbsolute { get; init; } = 12;

    /// <summary>
    /// Gets the target OOS sample count for full-strength model eligibility.
    /// Unit: walk-forward predictions for the same horizon.
    /// </summary>
    public int TargetOosSamplesForEligibility { get; init; } = 30;

    /// <summary>
    /// Gets the fraction of available OOS samples required for eligibility when evidence is scarce.
    /// </summary>
    public double MinimumOosSampleFraction { get; init; } = 0.45d;

    /// <summary>
    /// Gets the minimum prior OOS predictions needed before fitting probability calibration.
    /// Unit: walk-forward predictions strictly before the evaluated sample.
    /// </summary>
    public int MinimumCalibrationSamples { get; init; } = 30;

    /// <summary>
    /// Gets the minimum terminal-return samples needed before publishing forecast quantiles.
    /// Unit: realized horizon returns.
    /// </summary>
    public int MinimumQuantileSamples { get; init; } = 30;

    /// <summary>
    /// Gets the maximum acceptable expected calibration error for ensemble entry.
    /// Unit: probability error.
    /// </summary>
    public double MaximumAcceptableEce { get; init; } = 0.18d;

    /// <summary>
    /// Gets the minimum Brier improvement required for ensemble entry.
    /// </summary>
    public double MinimumBrierImprovement { get; init; } = 0.005d;

    /// <summary>
    /// Gets the lower bootstrap-skill bound tolerated for ensemble entry.
    /// Unit: Brier-score improvement versus baseline.
    /// </summary>
    public double MinimumBootstrapSkillLowerBound { get; init; } = 0d;

    /// <summary>
    /// Gets the robust RMS z-distance at which a state is considered out-of-distribution.
    /// Unit: standardized feature-space distance.
    /// </summary>
    public double OutOfDistributionThreshold { get; init; } = 3.5d;

    /// <summary>
    /// Gets the robust RMS z-distance at which a state is considered slightly unusual.
    /// Unit: standardized feature-space distance.
    /// </summary>
    public double SlightlyUnusualThreshold { get; init; } = 2.0d;

    /// <summary>
    /// Gets the preferred observation horizon used only as a soft primary-horizon tiebreaker.
    /// </summary>
    public int PreferredPrimaryHorizonObservations { get; init; } = 60;

    /// <summary>
    /// Gets the minimum spectral reliability for spectral timing features.
    /// </summary>
    public double MinimumSpectralReliability { get; init; } = 0.45d;

    /// <summary>
    /// Gets the minimum ensemble reliability required for directional timing zones.
    /// </summary>
    public double MinimumDirectionalReliability { get; init; } = 0.35d;

    /// <summary>
    /// Gets the deterministic seed persisted in diagnostics for reproducible stochastic extensions.
    /// </summary>
    public int Seed { get; init; } = 1729;

    /// <summary>
    /// Gets classifier options.
    /// </summary>
    public MarketEventClassifierOptions ClassifierOptions { get; init; } = new() { Iterations = 24 };
}
