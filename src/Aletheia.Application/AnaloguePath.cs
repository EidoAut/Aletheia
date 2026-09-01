namespace Aletheia.Application;

/// <summary>
/// Stores a normalized future path following one analogue state.
/// </summary>
public sealed record AnaloguePath(DateOnly StartDate, double Distance, IReadOnlyList<AnaloguePathPoint> Points);
