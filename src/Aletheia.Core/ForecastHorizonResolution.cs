namespace Aletheia.Core;

/// <summary>
/// Records how a requested horizon maps to observation-index work.
/// </summary>
public sealed record ForecastHorizonResolution
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForecastHorizonResolution"/> class.
    /// </summary>
    /// <param name="requestedHorizon">The requested horizon.</param>
    /// <param name="observationFrequency">The observation frequency used during resolution.</param>
    /// <param name="effectiveObservationCount">The number of observation steps used internally.</param>
    /// <param name="targetDate">The effective target date, when known.</param>
    /// <param name="resolutionPolicyName">The calendar or sampling policy name.</param>
    /// <param name="isApproximation">A value indicating whether generic cadence assumptions were used.</param>
    public ForecastHorizonResolution(
        ForecastHorizon requestedHorizon,
        ObservationFrequency observationFrequency,
        int effectiveObservationCount,
        DateOnly? targetDate,
        string resolutionPolicyName,
        bool isApproximation)
    {
        if (effectiveObservationCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(effectiveObservationCount),
                effectiveObservationCount,
                "Effective observation count cannot be negative.");
        }

        this.RequestedHorizon = requestedHorizon;
        this.ObservationFrequency = observationFrequency;
        this.EffectiveObservationCount = effectiveObservationCount;
        this.TargetDate = targetDate;
        this.ResolutionPolicyName = string.IsNullOrWhiteSpace(resolutionPolicyName)
            ? throw new ArgumentException("Resolution policy name cannot be empty.", nameof(resolutionPolicyName))
            : resolutionPolicyName;
        this.IsApproximation = isApproximation;
    }

    /// <summary>
    /// Gets the user- or model-requested horizon.
    /// </summary>
    public ForecastHorizon RequestedHorizon { get; }

    /// <summary>
    /// Gets the observation frequency used to resolve the horizon.
    /// </summary>
    public ObservationFrequency ObservationFrequency { get; }

    /// <summary>
    /// Gets the number of observation steps used by observation-index algorithms.
    /// </summary>
    public int EffectiveObservationCount { get; }

    /// <summary>
    /// Gets the target date, when calendar resolution can identify it.
    /// </summary>
    public DateOnly? TargetDate { get; }

    /// <summary>
    /// Gets the calendar or sampling policy used to resolve the horizon.
    /// </summary>
    public string ResolutionPolicyName { get; }

    /// <summary>
    /// Gets a value indicating whether generic cadence assumptions were used.
    /// </summary>
    public bool IsApproximation { get; }
}
