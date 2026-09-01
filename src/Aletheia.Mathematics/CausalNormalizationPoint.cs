namespace Aletheia.Mathematics;

/// <summary>
/// Stores one causally normalized observation and the statistics available at that instant.
/// </summary>
/// <param name="Index">The original observation index.</param>
/// <param name="RawValue">The raw input value.</param>
/// <param name="NormalizedValue">The normalized value.</param>
/// <param name="Location">The causal mean or median used for centering.</param>
/// <param name="Scale">The causal standard deviation or MAD-derived scale.</param>
/// <param name="SampleCount">The number of observations used to estimate the statistics.</param>
/// <param name="IsAvailable">A value indicating whether the normalized value is statistically available.</param>
public sealed record CausalNormalizationPoint(
    int Index,
    double RawValue,
    double NormalizedValue,
    double Location,
    double Scale,
    int SampleCount,
    bool IsAvailable);
