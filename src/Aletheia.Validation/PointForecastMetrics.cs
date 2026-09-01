namespace Aletheia.Validation;

/// <summary>
/// Summarizes point-forecast error metrics in decimal return units.
/// </summary>
public sealed record PointForecastMetrics(
    MetricStatus Status,
    int SampleCount,
    double? MeanAbsoluteError,
    double? MeanSquaredError,
    double? RootMeanSquaredError,
    double? DirectionalAccuracy);
