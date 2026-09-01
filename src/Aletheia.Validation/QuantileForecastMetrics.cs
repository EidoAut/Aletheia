namespace Aletheia.Validation;

/// <summary>
/// Summarizes pinball loss by forecast quantile.
/// </summary>
public sealed record QuantileForecastMetrics(
    MetricStatus Status,
    IReadOnlyDictionary<int, double> MeanPinballLossByPercentile,
    IReadOnlyDictionary<int, int> SampleCountByPercentile);
