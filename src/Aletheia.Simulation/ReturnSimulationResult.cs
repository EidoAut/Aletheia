using Aletheia.Forecasting;

namespace Aletheia.Simulation;

/// <summary>
/// Stores simulated horizon return samples.
/// </summary>
/// <param name="Method">The simulation method.</param>
/// <param name="Samples">The cumulative simple-return samples.</param>
/// <param name="Distribution">The resulting forecast-style distribution summary.</param>
/// <param name="Diagnostic">The simulation diagnostic.</param>
public sealed record ReturnSimulationResult(
    ReturnSimulationMethod Method,
    IReadOnlyList<double> Samples,
    ForecastDistribution Distribution,
    string Diagnostic);
