namespace Aletheia.Application;

/// <summary>
/// Summarizes a historical return distribution for presentation.
/// </summary>
public sealed record DistributionSummary(
    double Mean,
    double Median,
    double StandardDeviation,
    double Minimum,
    double Maximum,
    IReadOnlyList<HistogramBin> Histogram);
