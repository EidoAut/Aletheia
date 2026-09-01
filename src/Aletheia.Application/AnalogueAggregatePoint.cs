namespace Aletheia.Application;

/// <summary>
/// Stores robust aggregate path diagnostics for a normalized analogue horizon.
/// </summary>
public sealed record AnalogueAggregatePoint(
    int ObservationOffset,
    int SampleCount,
    double P25,
    double Median,
    double P75);
