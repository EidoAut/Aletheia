namespace Aletheia.Spectral;

/// <summary>
/// Defines the window function applied before FFT.
/// </summary>
public enum WindowFunction
{
    /// <summary>
    /// No window is applied.
    /// </summary>
    None,

    /// <summary>
    /// Applies a Hann window to reduce spectral leakage.
    /// </summary>
    Hann,
}
