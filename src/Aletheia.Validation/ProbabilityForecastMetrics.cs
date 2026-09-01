namespace Aletheia.Validation;

/// <summary>
/// Summarizes probability-positive scoring and calibration diagnostics.
/// </summary>
public sealed record ProbabilityForecastMetrics(
    MetricStatus Status,
    int SampleCount,
    double? BrierScore,
    double? ExpectedCalibrationError,
    IReadOnlyList<CalibrationBin> CalibrationBins);
