using Aletheia.Mathematics;

namespace Aletheia.Spectral;

/// <summary>
/// Estimates whether a dominant spectral period persists across rolling windows.
/// </summary>
public sealed class RollingSpectralStabilityAnalyzer
{
    private readonly PowerSpectrumAnalyzer analyzer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RollingSpectralStabilityAnalyzer"/> class.
    /// </summary>
    /// <param name="analyzer">The spectral analyzer used per window.</param>
    public RollingSpectralStabilityAnalyzer(PowerSpectrumAnalyzer? analyzer = null)
    {
        this.analyzer = analyzer ?? new PowerSpectrumAnalyzer();
    }

    /// <summary>
    /// Calculates simple rolling dominant-period stability diagnostics.
    /// </summary>
    /// <param name="samples">The ordered observation-index samples.</param>
    /// <param name="windowSize">The number of observations per window.</param>
    /// <param name="stepSize">The number of observations to advance between windows.</param>
    /// <param name="options">The spectral preprocessing options.</param>
    /// <param name="relativePeriodTolerance">The relative tolerance used for persistence.</param>
    /// <returns>The rolling stability result.</returns>
    public RollingSpectralStabilityResult Analyze(
        IReadOnlyList<double> samples,
        int windowSize,
        int stepSize,
        SpectralAnalysisOptions? options = null,
        double relativePeriodTolerance = 0.15d)
    {
        ArgumentNullException.ThrowIfNull(samples);

        if (windowSize < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(windowSize), windowSize, "Window size must be at least four observations.");
        }

        if (stepSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stepSize), stepSize, "Step size must be positive.");
        }

        if (samples.Count < windowSize)
        {
            return new RollingSpectralStabilityResult(0, 0d, 0d, 0d);
        }

        var full = this.analyzer.Analyze(samples, options);
        var fullPeriod = full.DominantFrequency?.PeriodObservations;
        var detectedPeriods = new List<double>();
        var detectedFrequencies = new List<double>();
        var windowCount = 0;
        var persistentCount = 0;

        for (var start = 0; start <= samples.Count - windowSize; start += stepSize)
        {
            windowCount++;
            var window = samples.Skip(start).Take(windowSize).ToArray();
            var result = this.analyzer.Analyze(window, options);
            var dominant = result.DominantFrequency;
            if (dominant is null)
            {
                continue;
            }

            detectedPeriods.Add(dominant.PeriodObservations);
            detectedFrequencies.Add(dominant.FrequencyCyclesPerObservation);
            if (fullPeriod.HasValue &&
                Math.Abs(dominant.PeriodObservations - fullPeriod.Value) / fullPeriod.Value <= relativePeriodTolerance)
            {
                persistentCount++;
            }
        }

        var detectionRate = windowCount == 0 ? 0d : detectedPeriods.Count / (double)windowCount;
        var persistence = detectedPeriods.Count == 0 ? 0d : persistentCount / (double)detectedPeriods.Count;
        var variance = detectedFrequencies.Count < 2 ? 0d : DescriptiveStatistics.SampleVariance(detectedFrequencies);

        return new RollingSpectralStabilityResult(windowCount, detectionRate, persistence, variance);
    }
}
