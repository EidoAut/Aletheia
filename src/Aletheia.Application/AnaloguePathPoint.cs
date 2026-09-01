namespace Aletheia.Application;

/// <summary>
/// Stores one normalized point on an analogue future path.
/// </summary>
public sealed record AnaloguePathPoint(int ObservationOffset, double Return);
