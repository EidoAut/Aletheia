namespace Aletheia.Spectral;

/// <summary>
/// Qualitative diagnostic strength of a dominant spectral peak.
/// </summary>
/// <remarks>
/// This is not a calibrated statistical confidence probability. It summarizes
/// peak concentration and sample adequacy only.
/// </remarks>
public enum SpectralDiagnosticStrength
{
    /// <summary>
    /// No meaningful dominant peak was detected.
    /// </summary>
    None,

    /// <summary>
    /// The peak is weak or based on limited data.
    /// </summary>
    Low,

    /// <summary>
    /// The peak is visible but requires stability and out-of-sample validation.
    /// </summary>
    Medium,

    /// <summary>
    /// The peak is concentrated within the observed power spectrum.
    /// </summary>
    High,
}
