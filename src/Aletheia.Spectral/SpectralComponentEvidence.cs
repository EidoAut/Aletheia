namespace Aletheia.Spectral;

/// <summary>
/// Describes evidence for one candidate spectral component.
/// </summary>
/// <param name="PeriodObservations">The period in observations.</param>
/// <param name="FrequencyCyclesPerObservation">The frequency in cycles per observation.</param>
/// <param name="RelativePower">Power as a fraction of total positive-frequency power.</param>
/// <param name="Stability">Rolling persistence near the candidate period.</param>
/// <param name="Phase">The phase angle in radians.</param>
/// <param name="Reliability">A conservative reliability score in [0, 1].</param>
/// <param name="Diagnostic">A human-readable evidence diagnostic.</param>
public sealed record SpectralComponentEvidence(
    double PeriodObservations,
    double FrequencyCyclesPerObservation,
    double RelativePower,
    double Stability,
    double Phase,
    double Reliability,
    string Diagnostic);
