using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Calculates empirical prediction interval coverage.
/// </summary>
public sealed class IntervalCoverageCalculator
{
    /// <summary>
    /// Calculates coverage for a percentile interval.
    /// </summary>
    /// <param name="samples">The evaluated predictions.</param>
    /// <param name="lowerPercentile">The lower percentile.</param>
    /// <param name="upperPercentile">The upper percentile.</param>
    /// <returns>Coverage metrics.</returns>
    public IntervalCoverageMetrics Calculate(
        IReadOnlyList<ForecastEvaluationSample> samples,
        int lowerPercentile = 10,
        int upperPercentile = 90)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (lowerPercentile < 0 || upperPercentile > 100 || lowerPercentile >= upperPercentile)
        {
            throw new ArgumentException("Coverage percentiles must satisfy 0 <= lower < upper <= 100.");
        }

        var nominal = (upperPercentile - lowerPercentile) / 100d;
        var count = 0;
        var covered = 0;
        var widthSum = 0d;
        foreach (var sample in samples)
        {
            if (!sample.Prediction.Prediction.Supports(ForecastCapabilities.Quantiles))
            {
                continue;
            }

            if (!sample.Prediction.Prediction.ReturnPercentiles.TryGetValue(lowerPercentile, out var lower) ||
                !sample.Prediction.Prediction.ReturnPercentiles.TryGetValue(upperPercentile, out var upper))
            {
                continue;
            }

            count++;
            widthSum += upper - lower;
            if (sample.Evaluation.ActualReturn >= lower && sample.Evaluation.ActualReturn <= upper)
            {
                covered++;
            }
        }

        if (count == 0)
        {
            return new IntervalCoverageMetrics(ResolveStatus(samples.Count, count), lowerPercentile, upperPercentile, nominal, 0, null, null, null);
        }

        var observed = covered / (double)count;
        return new IntervalCoverageMetrics(
            MetricStatus.Available,
            lowerPercentile,
            upperPercentile,
            nominal,
            count,
            observed,
            observed - nominal,
            widthSum / count);
    }

    private static MetricStatus ResolveStatus(int totalSamples, int supportedSamples)
    {
        if (supportedSamples > 0)
        {
            return MetricStatus.Available;
        }

        return totalSamples == 0 ? MetricStatus.NoSamples : MetricStatus.NotSupported;
    }
}
