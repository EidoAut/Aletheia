namespace Aletheia.Spectral;

/// <summary>
/// Converts spectrum and rolling stability diagnostics into conservative component evidence.
/// </summary>
public sealed class SpectralEvidenceAnalyzer
{
    /// <summary>
    /// Builds spectral evidence for the dominant candidate component.
    /// </summary>
    /// <param name="spectrum">The full-sample spectrum.</param>
    /// <param name="stability">The rolling stability result.</param>
    /// <returns>Candidate evidence, or <see langword="null"/> when no component is available.</returns>
    public SpectralComponentEvidence? AnalyzeDominant(
        SpectralAnalysisResult spectrum,
        RollingSpectralStabilityResult stability)
    {
        ArgumentNullException.ThrowIfNull(spectrum);
        ArgumentNullException.ThrowIfNull(stability);

        if (spectrum.DominantFrequency is null || spectrum.Bins.Count == 0)
        {
            return null;
        }

        var stabilityScore = Math.Clamp(stability.DominantPeriodPersistence, 0d, 1d);
        var powerScore = Math.Clamp(spectrum.PeakPowerFraction, 0d, 1d);
        var reliability = Math.Clamp(Math.Sqrt(powerScore * stabilityScore), 0d, 1d);
        var diagnostic = reliability switch
        {
            >= 0.70d => "Stable high-power spectral candidate; still requires OOS validation before forecasting use.",
            >= 0.40d => "Moderate spectral candidate; do not treat as predictive without OOS improvement.",
            _ => "Weak spectral candidate; likely descriptive only.",
        };

        return new SpectralComponentEvidence(
            spectrum.DominantFrequency.PeriodObservations,
            spectrum.DominantFrequency.FrequencyCyclesPerObservation,
            spectrum.PeakPowerFraction,
            stabilityScore,
            spectrum.DominantFrequency.Phase,
            reliability,
            diagnostic);
    }
}
