namespace Aletheia.Spectral;

/// <summary>
/// Contains an observation-index power-spectrum analysis and dominant component diagnostics.
/// </summary>
public sealed class SpectralAnalysisResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpectralAnalysisResult"/> class.
    /// </summary>
    /// <param name="bins">The positive-frequency bins.</param>
    /// <param name="dominantFrequency">The dominant frequency bin, when available.</param>
    /// <param name="peakPowerFraction">The dominant peak power divided by total positive-frequency power.</param>
    /// <param name="peakToBackgroundRatio">The dominant peak power divided by mean non-dominant power.</param>
    /// <param name="diagnosticStrength">The qualitative diagnostic strength.</param>
    /// <param name="options">The preprocessing options used.</param>
    /// <param name="originalSampleCount">The number of actual signal samples before padding.</param>
    /// <param name="transformLength">The FFT length after optional padding.</param>
    /// <param name="zeroPaddingApplied">A value indicating whether zero-padding was applied.</param>
    /// <param name="coherentGain">The coherent gain of the applied window.</param>
    public SpectralAnalysisResult(
        IReadOnlyList<FrequencyBin> bins,
        FrequencyBin? dominantFrequency,
        double peakPowerFraction,
        double peakToBackgroundRatio,
        SpectralDiagnosticStrength diagnosticStrength,
        SpectralAnalysisOptions options,
        int originalSampleCount,
        int transformLength,
        bool zeroPaddingApplied,
        double coherentGain)
    {
        this.Bins = bins;
        this.DominantFrequency = dominantFrequency;
        this.PeakPowerFraction = peakPowerFraction;
        this.PeakToBackgroundRatio = peakToBackgroundRatio;
        this.DiagnosticStrength = diagnosticStrength;
        this.Options = options;
        this.OriginalSampleCount = originalSampleCount;
        this.TransformLength = transformLength;
        this.ZeroPaddingApplied = zeroPaddingApplied;
        this.CoherentGain = coherentGain;
    }

    /// <summary>
    /// Gets the positive-frequency bins.
    /// </summary>
    public IReadOnlyList<FrequencyBin> Bins { get; }

    /// <summary>
    /// Gets the dominant non-zero frequency bin, when available.
    /// </summary>
    public FrequencyBin? DominantFrequency { get; }

    /// <summary>
    /// Gets dominant peak power divided by total positive-frequency power.
    /// </summary>
    public double PeakPowerFraction { get; }

    /// <summary>
    /// Gets dominant peak power divided by mean non-dominant power.
    /// </summary>
    public double PeakToBackgroundRatio { get; }

    /// <summary>
    /// Gets the qualitative diagnostic strength.
    /// </summary>
    public SpectralDiagnosticStrength DiagnosticStrength { get; }

    /// <summary>
    /// Gets the preprocessing options used before FFT.
    /// </summary>
    public SpectralAnalysisOptions Options { get; }

    /// <summary>
    /// Gets the number of actual signal samples before padding.
    /// </summary>
    public int OriginalSampleCount { get; }

    /// <summary>
    /// Gets the FFT length after optional zero-padding.
    /// </summary>
    public int TransformLength { get; }

    /// <summary>
    /// Gets a value indicating whether zero-padding was applied.
    /// </summary>
    public bool ZeroPaddingApplied { get; }

    /// <summary>
    /// Gets the coherent gain of the applied window.
    /// </summary>
    public double CoherentGain { get; }
}
