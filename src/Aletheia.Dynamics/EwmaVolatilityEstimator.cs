using Aletheia.Mathematics;

namespace Aletheia.Dynamics;

/// <summary>
/// Estimates dynamic volatility with an exponentially weighted moving variance.
/// </summary>
public sealed class EwmaVolatilityEstimator
{
    /// <summary>
    /// Estimates the EWMA variance path from finite residuals or returns.
    /// </summary>
    /// <param name="values">The finite residual or return observations.</param>
    /// <param name="lambda">The decay parameter in (0, 1).</param>
    /// <returns>The EWMA volatility result.</returns>
    public EwmaVolatilityResult Estimate(IReadOnlyList<double> values, double lambda = 0.94d)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (!double.IsFinite(lambda) || lambda <= 0d || lambda >= 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(lambda), lambda, "Lambda must be finite and strictly between 0 and 1.");
        }

        if (values.Count == 0)
        {
            return new EwmaVolatilityResult(lambda, Array.Empty<double>(), 0d, 0d);
        }

        for (var index = 0; index < values.Count; index++)
        {
            if (!double.IsFinite(values[index]))
            {
                throw new ArgumentException("EWMA volatility requires finite observations.", nameof(values));
            }
        }

        var initialVariance = values.Count < 2
            ? values[0] * values[0]
            : DescriptiveStatistics.SampleVariance(values);
        initialVariance = Math.Max(0d, initialVariance);
        var variances = new double[values.Count];
        variances[0] = initialVariance;

        for (var index = 1; index < values.Count; index++)
        {
            variances[index] = (lambda * variances[index - 1]) + ((1d - lambda) * values[index - 1] * values[index - 1]);
        }

        var lastVariance = variances[^1];
        return new EwmaVolatilityResult(lambda, variances, lastVariance, Math.Sqrt(lastVariance));
    }
}
