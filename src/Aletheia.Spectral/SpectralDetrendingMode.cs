namespace Aletheia.Spectral;

/// <summary>
/// Defines the trend treatment applied before spectral analysis.
/// </summary>
public enum SpectralDetrendingMode
{
    /// <summary>
    /// No trend component is removed.
    /// </summary>
    None,

    /// <summary>
    /// Only the sample mean is removed.
    /// </summary>
    MeanRemoval,

    /// <summary>
    /// A fitted linear trend is removed.
    /// </summary>
    LinearDetrend,
}
