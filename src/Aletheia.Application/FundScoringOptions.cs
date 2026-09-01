namespace Aletheia.Application;

/// <summary>
/// Centralizes fund-scoring weights and thresholds.
/// </summary>
public sealed record FundScoringOptions
{
    /// <summary>
    /// Gets the performance quality weight.
    /// </summary>
    public double PerformanceQualityWeight { get; init; } = 0.22d;

    /// <summary>
    /// Gets the risk quality weight.
    /// </summary>
    public double RiskQualityWeight { get; init; } = 0.22d;

    /// <summary>
    /// Gets the risk-adjusted performance weight.
    /// </summary>
    public double RiskAdjustedPerformanceWeight { get; init; } = 0.18d;

    /// <summary>
    /// Gets the stability weight.
    /// </summary>
    public double StabilityWeight { get; init; } = 0.14d;

    /// <summary>
    /// Gets the predictive evidence weight.
    /// </summary>
    public double PredictiveEvidenceWeight { get; init; } = 0.12d;

    /// <summary>
    /// Gets the data quality weight.
    /// </summary>
    public double DataQualityWeight { get; init; } = 0.12d;

    /// <summary>
    /// Gets the high-confidence observation threshold.
    /// </summary>
    public int HighConfidenceObservationCount { get; init; } = 1_000;

    /// <summary>
    /// Gets the medium-confidence observation threshold.
    /// </summary>
    public int MediumConfidenceObservationCount { get; init; } = 250;

    /// <summary>
    /// Gets the annual volatility level considered low for fund scoring.
    /// </summary>
    public double LowAnnualVolatility { get; init; } = 0.06d;

    /// <summary>
    /// Gets the annual volatility level considered high for fund scoring.
    /// </summary>
    public double HighAnnualVolatility { get; init; } = 0.25d;

    /// <summary>
    /// Gets the maximum drawdown level considered mild.
    /// </summary>
    public double MildMaximumDrawdown { get; init; } = 0.05d;

    /// <summary>
    /// Gets the maximum drawdown level considered severe.
    /// </summary>
    public double SevereMaximumDrawdown { get; init; } = 0.35d;

    /// <summary>
    /// Gets the minimum predictive reliability needed for a non-neutral signal.
    /// </summary>
    public double MinimumSignalReliability { get; init; } = 0.45d;
}
