namespace Aletheia.Application;

/// <summary>
/// Stores current forecast outputs across available models and horizons.
/// </summary>
public sealed record ForecastCollectionResult(IReadOnlyList<ForecastModelRun> Runs);
