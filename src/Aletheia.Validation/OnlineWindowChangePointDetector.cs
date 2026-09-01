namespace Aletheia.Validation;

/// <summary>
/// Provides a deterministic online structural-change probability approximation
/// from two adjacent causal windows.
/// </summary>
public sealed class OnlineWindowChangePointDetector
{
    /// <summary>
    /// Estimates an online change-point probability path.
    /// </summary>
    /// <param name="values">The ordered finite observations.</param>
    /// <param name="window">The causal comparison window.</param>
    /// <returns>Change-point probability points.</returns>
    public IReadOnlyList<ChangePointProbabilityPoint> Estimate(IReadOnlyList<double> values, int window = 40)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (window < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(window), window, "Window must be at least four observations.");
        }

        var result = new List<ChangePointProbabilityPoint>(values.Count);
        for (var index = 0; index < values.Count; index++)
        {
            if (!double.IsFinite(values[index]))
            {
                throw new ArgumentException("Change-point detection requires finite observations.", nameof(values));
            }

            if (index < (window * 2) - 1)
            {
                result.Add(new ChangePointProbabilityPoint(index, 0d, 0));
                continue;
            }

            var leftMean = Mean(values, index - (2 * window) + 1, window);
            var rightMean = Mean(values, index - window + 1, window);
            var leftVariance = Variance(values, index - (2 * window) + 1, window, leftMean);
            var rightVariance = Variance(values, index - window + 1, window, rightMean);
            var pooledStandardError = Math.Sqrt(Math.Max(1e-12d, (leftVariance / window) + (rightVariance / window)));
            var zScore = Math.Abs(rightMean - leftMean) / pooledStandardError;
            var probability = 1d / (1d + Math.Exp(-(zScore - 3d)));
            result.Add(new ChangePointProbabilityPoint(index, Math.Clamp(probability, 0d, 1d), window));
        }

        return result;
    }

    private static double Mean(IReadOnlyList<double> values, int start, int count)
    {
        var sum = 0d;
        for (var index = start; index < start + count; index++)
        {
            sum += values[index];
        }

        return sum / count;
    }

    private static double Variance(IReadOnlyList<double> values, int start, int count, double mean)
    {
        var sum = 0d;
        for (var index = start; index < start + count; index++)
        {
            var deviation = values[index] - mean;
            sum += deviation * deviation;
        }

        return sum / Math.Max(1, count - 1);
    }
}
