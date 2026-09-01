using Aletheia.Validation;

namespace Aletheia.Application;

/// <summary>
/// Centralizes market-timing interpretation thresholds.
/// </summary>
public sealed record MarketTimingPresentationOptions
{
    /// <summary>
    /// Gets the edge required for a watch-positive or watch-negative zone.
    /// </summary>
    public double WatchEdge { get; init; } = 0.08d;

    /// <summary>
    /// Gets the edge required for accumulation or reduction.
    /// </summary>
    public double DirectionalEdge { get; init; } = 0.18d;

    /// <summary>
    /// Gets the edge required for strong accumulation or strong reduction.
    /// </summary>
    public double StrongEdge { get; init; } = 0.30d;

    /// <summary>
    /// Gets the minimum reliability for a directional zone.
    /// </summary>
    public double MinimumDirectionalReliability { get; init; } = 0.35d;

    /// <summary>
    /// Gets the risk penalty used for risk-adjusted utility.
    /// </summary>
    public double RiskPenaltyLambda { get; init; } = 0.5d;

    /// <summary>
    /// Gets the minimum predictive evidence required for non-neutral action.
    /// </summary>
    public EvidenceStrength MinimumDirectionalEvidence { get; init; } = EvidenceStrength.Weak;
}
