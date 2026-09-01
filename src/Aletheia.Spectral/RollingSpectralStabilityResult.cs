namespace Aletheia.Spectral;

/// <summary>
/// Summarizes dominant-period stability across rolling spectral windows.
/// </summary>
public sealed class RollingSpectralStabilityResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RollingSpectralStabilityResult"/> class.
    /// </summary>
    /// <param name="windowCount">The number of rolling windows inspected.</param>
    /// <param name="windowDetectionRate">The fraction of windows with a detected dominant component.</param>
    /// <param name="dominantPeriodPersistence">The fraction of detected windows near the full-history dominant period.</param>
    /// <param name="dominantFrequencyVariance">The sample variance of detected dominant frequencies.</param>
    public RollingSpectralStabilityResult(
        int windowCount,
        double windowDetectionRate,
        double dominantPeriodPersistence,
        double dominantFrequencyVariance)
    {
        this.WindowCount = windowCount;
        this.WindowDetectionRate = windowDetectionRate;
        this.DominantPeriodPersistence = dominantPeriodPersistence;
        this.DominantFrequencyVariance = dominantFrequencyVariance;
    }

    /// <summary>
    /// Gets the number of rolling windows inspected.
    /// </summary>
    public int WindowCount { get; }

    /// <summary>
    /// Gets the fraction of windows with a detected dominant component.
    /// </summary>
    public double WindowDetectionRate { get; }

    /// <summary>
    /// Gets the fraction of detected windows whose period is near the full-history dominant period.
    /// </summary>
    public double DominantPeriodPersistence { get; }

    /// <summary>
    /// Gets the sample variance of dominant frequencies across detected windows.
    /// </summary>
    public double DominantFrequencyVariance { get; }
}
