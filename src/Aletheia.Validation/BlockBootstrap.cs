namespace Aletheia.Validation;

/// <summary>
/// Provides deterministic block-bootstrap intervals for dependent timing samples.
/// </summary>
public static class BlockBootstrap
{
    /// <summary>
    /// Estimates a confidence interval for mean values with moving blocks.
    /// </summary>
    /// <param name="values">The sample values.</param>
    /// <param name="blockSize">The contiguous block size.</param>
    /// <param name="replications">The number of bootstrap replications.</param>
    /// <param name="seed">The deterministic seed.</param>
    /// <param name="confidenceLevel">The confidence level.</param>
    /// <returns>The interval.</returns>
    public static ProbabilityInterval MeanInterval(
        IReadOnlyList<double> values,
        int blockSize = 5,
        int replications = 300,
        int seed = 1729,
        double confidenceLevel = 0.90d)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            return new ProbabilityInterval(0d, 0d);
        }

        if (blockSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockSize), blockSize, "Block size must be positive.");
        }

        if (replications <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(replications), replications, "Replications must be positive.");
        }

        var random = new Random(seed);
        var estimates = new double[replications];
        for (var replication = 0; replication < replications; replication++)
        {
            var sum = 0d;
            var count = 0;
            while (count < values.Count)
            {
                var start = random.Next(values.Count);
                for (var offset = 0; offset < blockSize && count < values.Count; offset++)
                {
                    sum += values[(start + offset) % values.Count];
                    count++;
                }
            }

            estimates[replication] = sum / values.Count;
        }

        Array.Sort(estimates);
        var alpha = (1d - confidenceLevel) / 2d;
        return new ProbabilityInterval(
            Quantile(estimates, alpha),
            Quantile(estimates, 1d - alpha));
    }

    private static double Quantile(IReadOnlyList<double> sorted, double probability)
    {
        if (sorted.Count == 1)
        {
            return sorted[0];
        }

        var position = Math.Clamp(probability, 0d, 1d) * (sorted.Count - 1);
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        var weight = position - lower;
        return sorted[lower] + ((sorted[upper] - sorted[lower]) * weight);
    }
}
