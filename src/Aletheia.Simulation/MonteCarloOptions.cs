namespace Aletheia.Simulation;

/// <summary>
/// Configures Monte Carlo return simulation.
/// </summary>
public sealed record MonteCarloOptions
{
    /// <summary>
    /// Gets the number of simulated paths.
    /// </summary>
    public int PathCount { get; init; } = 5_000;

    /// <summary>
    /// Gets the deterministic random seed.
    /// </summary>
    public int Seed { get; init; } = 161803;
}
