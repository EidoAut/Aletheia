namespace Aletheia.Mathematics;

/// <summary>
/// Normalizes ordered observations using only information available at each observation.
/// </summary>
public sealed class CausalNormalizer
{
    private const double RobustMadConsistencyFactor = 1.4826d;

    /// <summary>
    /// Applies causal normalization to a finite ordered sequence.
    /// </summary>
    /// <param name="values">The ordered finite observations.</param>
    /// <param name="mode">The causal normalization mode.</param>
    /// <param name="windowSize">The rolling window size for rolling modes.</param>
    /// <param name="minimumSamples">The minimum samples required to emit an available value.</param>
    /// <returns>Causal normalization points aligned to <paramref name="values"/>.</returns>
    public IReadOnlyList<CausalNormalizationPoint> Normalize(
        IReadOnlyList<double> values,
        CausalNormalizationMode mode,
        int windowSize = 252,
        int minimumSamples = 2)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (windowSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowSize), windowSize, "Window size must be positive.");
        }

        if (minimumSamples <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumSamples), minimumSamples, "Minimum samples must be positive.");
        }

        var result = new List<CausalNormalizationPoint>(values.Count);
        for (var index = 0; index < values.Count; index++)
        {
            DescriptiveStatistics.EnsureFinite(values[index], nameof(values));
            var startIndex = mode == CausalNormalizationMode.ExpandingZScore
                ? 0
                : Math.Max(0, index - windowSize + 1);
            var window = Slice(values, startIndex, index);
            var sampleCount = window.Length;
            if (sampleCount < minimumSamples)
            {
                result.Add(new CausalNormalizationPoint(index, values[index], 0d, values[index], 0d, sampleCount, false));
                continue;
            }

            var location = mode == CausalNormalizationMode.RollingRobust
                ? DescriptiveStatistics.Median(window)
                : DescriptiveStatistics.Mean(window);
            var scale = mode == CausalNormalizationMode.RollingRobust
                ? DescriptiveStatistics.MedianAbsoluteDeviation(window) * RobustMadConsistencyFactor
                : DescriptiveStatistics.SampleStandardDeviation(window);

            if (scale == 0d)
            {
                result.Add(new CausalNormalizationPoint(index, values[index], 0d, location, scale, sampleCount, true));
                continue;
            }

            result.Add(new CausalNormalizationPoint(index, values[index], (values[index] - location) / scale, location, scale, sampleCount, true));
        }

        return result;
    }

    private static double[] Slice(IReadOnlyList<double> values, int startIndex, int endIndex)
    {
        var length = endIndex - startIndex + 1;
        var result = new double[length];
        for (var index = 0; index < length; index++)
        {
            result[index] = values[startIndex + index];
        }

        return result;
    }
}
