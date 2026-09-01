namespace Aletheia.Spectral;

/// <summary>
/// Describes one observation-index component from a discrete power spectrum.
/// </summary>
public sealed class FrequencyBin
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FrequencyBin"/> class.
    /// </summary>
    /// <param name="frequencyCyclesPerObservation">The frequency in cycles per observation.</param>
    /// <param name="periodObservations">The corresponding period in observations.</param>
    /// <param name="amplitude">The amplitude estimate.</param>
    /// <param name="power">The power estimate.</param>
    /// <param name="phase">The phase angle in radians.</param>
    public FrequencyBin(
        double frequencyCyclesPerObservation,
        double periodObservations,
        double amplitude,
        double power,
        double phase)
    {
        this.FrequencyCyclesPerObservation = frequencyCyclesPerObservation;
        this.PeriodObservations = periodObservations;
        this.Amplitude = amplitude;
        this.Power = power;
        this.Phase = phase;
    }

    /// <summary>
    /// Gets the frequency in cycles per observation.
    /// </summary>
    public double FrequencyCyclesPerObservation { get; }

    /// <summary>
    /// Gets the corresponding period in observations.
    /// </summary>
    public double PeriodObservations { get; }

    /// <summary>
    /// Gets the amplitude estimate.
    /// </summary>
    public double Amplitude { get; }

    /// <summary>
    /// Gets the power estimate.
    /// </summary>
    public double Power { get; }

    /// <summary>
    /// Gets the phase angle in radians.
    /// </summary>
    public double Phase { get; }
}
