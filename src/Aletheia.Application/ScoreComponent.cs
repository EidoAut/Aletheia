namespace Aletheia.Application;

/// <summary>
/// Stores one weighted score component.
/// </summary>
/// <param name="Name">The component name.</param>
/// <param name="Score">The component score in [1, 10].</param>
/// <param name="Weight">The normalized component weight.</param>
/// <param name="Reason">The deterministic explanation.</param>
public sealed record ScoreComponent(
    string Name,
    double Score,
    double Weight,
    string Reason);
