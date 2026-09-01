namespace Aletheia.Validation;

/// <summary>
/// Summarizes empirical coverage of a central prediction interval.
/// </summary>
public sealed record IntervalCoverageMetrics(
    MetricStatus Status,
    int LowerPercentile,
    int UpperPercentile,
    double NominalCoverage,
    int SampleCount,
    double? ObservedCoverage,
    double? CoverageError,
    double? AverageIntervalWidth);
