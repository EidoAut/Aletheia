namespace Aletheia.Dynamics;

/// <summary>
/// Stores one historical state matched to the current state.
/// </summary>
public sealed class HistoricalAnalogueResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HistoricalAnalogueResult"/> class.
    /// </summary>
    /// <param name="observation">The matched historical state.</param>
    /// <param name="distance">The standardized Euclidean distance.</param>
    public HistoricalAnalogueResult(StateObservation observation, double distance)
    {
        this.Observation = observation ?? throw new ArgumentNullException(nameof(observation));
        this.Distance = distance;
    }

    /// <summary>
    /// Gets the matched historical state.
    /// </summary>
    public StateObservation Observation { get; }

    /// <summary>
    /// Gets the standardized Euclidean distance.
    /// </summary>
    public double Distance { get; }
}
