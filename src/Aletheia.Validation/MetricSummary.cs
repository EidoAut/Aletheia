namespace Aletheia.Validation;

/// <summary>
/// Groups all validation metrics calculated from one set of evaluated predictions.
/// </summary>
public sealed record MetricSummary(
    PointForecastMetrics Point,
    ProbabilityForecastMetrics Probability,
    QuantileForecastMetrics Quantile,
    IntervalCoverageMetrics IntervalCoverage);
