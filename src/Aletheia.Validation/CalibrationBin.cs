namespace Aletheia.Validation;

/// <summary>
/// Stores empirical calibration statistics for one probability bin.
/// </summary>
public sealed record CalibrationBin(
    double LowerBoundInclusive,
    double UpperBoundInclusive,
    int SampleCount,
    double? MeanPredictedProbability,
    double? ObservedPositiveFrequency);
