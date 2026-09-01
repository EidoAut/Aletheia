using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Calculates probability calibration bins and expected calibration error.
/// </summary>
public sealed class CalibrationCalculator
{
    /// <summary>
    /// Calculates calibration diagnostics.
    /// </summary>
    /// <param name="samples">The evaluated predictions.</param>
    /// <param name="options">Calibration options.</param>
    /// <returns>Probability metrics including calibration bins.</returns>
    public ProbabilityForecastMetrics Calculate(
        IReadOnlyList<ForecastEvaluationSample> samples,
        CalibrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var probabilitySamples = samples
            .Where(sample => sample.Prediction.Prediction.Supports(ForecastCapabilities.ProbabilityPositive))
            .ToArray();
        var bins = new List<CalibrationBin>(options.BinCount);
        var total = probabilitySamples.Length;
        var expectedCalibrationError = 0d;
        for (var index = 0; index < options.BinCount; index++)
        {
            var lower = index / (double)options.BinCount;
            var upper = (index + 1) / (double)options.BinCount;
            var binSamples = probabilitySamples
                .Where(sample =>
                    IsInBin(sample.Prediction.Prediction.ProbabilityPositive, lower, upper, index == options.BinCount - 1))
                .ToArray();
            if (binSamples.Length == 0)
            {
                bins.Add(new CalibrationBin(lower, upper, 0, null, null));
                continue;
            }

            var meanProbability = binSamples.Average(sample => sample.Prediction.Prediction.ProbabilityPositive);
            var observedFrequency = binSamples.Average(sample => sample.Evaluation.ProbabilityOutcome);
            expectedCalibrationError += (binSamples.Length / (double)total) * Math.Abs(observedFrequency - meanProbability);
            bins.Add(new CalibrationBin(lower, upper, binSamples.Length, meanProbability, observedFrequency));
        }

        var brier = new BrierScoreCalculator().Calculate(probabilitySamples.Select(sample => sample.Evaluation).ToArray());
        return new ProbabilityForecastMetrics(
            ResolveStatus(samples.Count, total),
            total,
            brier,
            total == 0 ? null : expectedCalibrationError,
            bins);
    }

    private static MetricStatus ResolveStatus(int totalSamples, int supportedSamples)
    {
        if (supportedSamples > 0)
        {
            return MetricStatus.Available;
        }

        return totalSamples == 0 ? MetricStatus.NoSamples : MetricStatus.NotSupported;
    }

    private static bool IsInBin(double probability, double lower, double upper, bool isLastBin)
    {
        return probability >= lower && (probability < upper || (isLastBin && probability <= upper));
    }
}
