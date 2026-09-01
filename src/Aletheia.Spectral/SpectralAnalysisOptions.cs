namespace Aletheia.Spectral;

/// <summary>
/// Configures observation-index spectral analysis.
/// </summary>
public sealed record SpectralAnalysisOptions
{
    /// <summary>
    /// Gets the detrending mode applied before windowing.
    /// </summary>
    public SpectralDetrendingMode DetrendingMode { get; init; } = SpectralDetrendingMode.LinearDetrend;

    /// <summary>
    /// Gets the window function applied before FFT.
    /// </summary>
    public WindowFunction Window { get; init; } = WindowFunction.Hann;

    /// <summary>
    /// Gets a value indicating whether samples should be zero-padded to the next power of two.
    /// </summary>
    public bool ZeroPadToPowerOfTwo { get; init; } = true;
}
